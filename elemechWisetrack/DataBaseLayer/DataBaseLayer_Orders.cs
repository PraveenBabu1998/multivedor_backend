using elemechWisetrack.Models;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Razorpay.Api;

namespace elemechWisetrack.DataBaseLayer
{
    public interface IDataBaseLayer_Orders
    {
        Task<object> GetCheckoutDetails(string email, string couponCode);
        Task<object> CreateOrder(string email, CreateOrderModel model);
        Task<object> VerifyPayment(RazorpayVerifyModel model);
        Task<object> GetUserOrders(string email);
        Task<object> CancelOrder(string email, Guid orderId);
        Task<object> RequestExchange(string email, ExchangeRequestModel model);
        Task<object> GetExchangeRequests(string email, bool isAdmin);
        Task<object> UpdateExchangeStatus(UpdateExchangeStatusModel model);
        Task<object> SchedulePickup(PickupRequestModel model);
        Task<object> CompleteExchange(Guid exchangeId);
        Task<object> UpdatePickupStatus(Guid exchangeId, string status);
        Task<object> GetMyOrders(string email);
        Task<object> UpdateOrderStatus(UpdateOrderStatusModel model);
        Task<object> TrackOrder(Guid orderId);
        Task<object> GetOrderDetails(string email, Guid orderId);
    }
    public partial interface IDataBaseLayer : IDataBaseLayer_Orders
    {
    }

    public partial class DataBaseLayer
    {

        public async Task<object> GetCheckoutDetails(string email, string couponCode)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            // ============================
            // 🔹 1. GET USER ID
            // ============================
            string getUserQuery = @"SELECT ""Id"" FROM ""AspNetUsers"" WHERE ""Email""=@Email LIMIT 1";
            Guid userId;

            using (var cmd = new NpgsqlCommand(getUserQuery, conn))
            {
                cmd.Parameters.AddWithValue("@Email", email);
                var result = await cmd.ExecuteScalarAsync();

                if (result == null)
                    return new { success = false, message = "User not found" };

                userId = Guid.Parse(result.ToString());
            }

            // ============================
            // 🔹 2. GET CART ITEMS
            // ============================
            List<CartProductDetail> cartItems = new();

            string cartQuery = @"
    SELECT ci.product_id, ci.quantity, p.name, p.discountprice, p.mainimage
    FROM cart_items ci
    JOIN carts c ON c.id = ci.cart_id
    JOIN products p ON p.id = ci.product_id
    WHERE c.user_id = @userId";

            using (var cmd = new NpgsqlCommand(cartQuery, conn))
            {
                cmd.Parameters.AddWithValue("@userId", userId);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    cartItems.Add(new CartProductDetail
                    {
                        ProductId = reader.GetGuid(0),
                        Quantity = reader.GetInt32(1),
                        ProductName = reader.GetString(2),
                        Price = reader.GetDecimal(3),
                        Image = reader.IsDBNull(4) ? null : reader.GetString(4)
                    });
                }
            }

            // ============================
            // 🔹 3. TOTAL
            // ============================
            decimal totalAmount = cartItems.Sum(x => x.Price * x.Quantity);

            // ============================
            // 🔹 4. APPLY COUPON (ONLY IF PROVIDED)
            // ============================
            decimal discount = 0;
            string appliedCoupon = null;

            if (!string.IsNullOrWhiteSpace(couponCode)) // ✅ FIX 1
            {
                string couponQuery = @"
        SELECT *
        FROM coupons
        WHERE code = @code
        AND is_active = TRUE";

                using var couponCmd = new NpgsqlCommand(couponQuery, conn);
                couponCmd.Parameters.AddWithValue("@code", couponCode);

                using var reader = await couponCmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var discountType = reader["discount_type"]?.ToString();
                    var discountValue = Convert.ToDecimal(reader["discount_value"]);
                    var minOrder = Convert.ToDecimal(reader["min_order_amount"]);

                    var startDate = Convert.ToDateTime(reader["start_date"]);
                    var endDate = Convert.ToDateTime(reader["end_date"]);

                    var maxDiscount = reader["max_discount_amount"] == DBNull.Value
                        ? (decimal?)null
                        : Convert.ToDecimal(reader["max_discount_amount"]);

                    // ✅ FIX 2: DATE VALIDATION
                    if (DateTime.UtcNow < startDate || DateTime.UtcNow > endDate)
                    {
                        return new
                        {
                            success = false,
                            message = "Coupon expired or not started yet"
                        };
                    }

                    // ✅ FIX 3: MIN ORDER CHECK
                    if (totalAmount < minOrder)
                    {
                        return new
                        {
                            success = false,
                            message = $"Minimum order should be {minOrder}"
                        };
                    }

                    // ✅ APPLY COUPON
                    if (discountType == "percentage")
                    {
                        discount = (totalAmount * discountValue) / 100;
                    }
                    else if (discountType == "fixed")
                    {
                        discount = discountValue;
                    }

                    // ✅ MAX CAP
                    if (maxDiscount.HasValue && discount > maxDiscount.Value)
                        discount = maxDiscount.Value;

                    appliedCoupon = couponCode;
                }
                else
                {
                    return new
                    {
                        success = false,
                        message = "Invalid coupon code"
                    };
                }

                await reader.CloseAsync();
            }

            // ============================
            // 🔹 FINAL
            // ============================
            decimal finalAmount = totalAmount - discount;

            return new
            {
                success = true,
                UserId = userId,
                Email = email,
                CartItems = cartItems,
                TotalAmount = totalAmount,
                Discount = discount,
                CouponCode = appliedCoupon,
                FinalAmount = finalAmount
            };
        }

        public async Task<object> CreateOrder(string email, CreateOrderModel model)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            using var transaction = await conn.BeginTransactionAsync();

            try
            {
                // ============================
                // 🔹 1. GET USER ID
                // ============================
                string getUserQuery = @"SELECT ""Id"" FROM ""AspNetUsers"" WHERE ""Email""=@Email LIMIT 1";

                Guid userId;

                using (var cmd = new NpgsqlCommand(getUserQuery, conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    var result = await cmd.ExecuteScalarAsync();

                    if (result == null)
                        return new { success = false, message = "User not found" };

                    userId = Guid.Parse(result.ToString());
                }

                // ============================
                // 🔹 2. VALIDATE ITEMS
                // ============================
                if (model.Items == null || !model.Items.Any())
                    return new { success = false, message = "No items provided" };

                var cartItems = new List<OrderItemModel>();

                foreach (var item in model.Items)
                {
                    string productQuery = "SELECT discountprice FROM products WHERE id=@pid";

                    using var cmd = new NpgsqlCommand(productQuery, conn, transaction);
                    cmd.Parameters.AddWithValue("@pid", item.ProductId);

                    var result = await cmd.ExecuteScalarAsync();

                    if (result == null)
                        return new { success = false, message = "Invalid product" };

                    decimal price = (decimal)result;

                    cartItems.Add(new OrderItemModel
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        Price = price,
                        Discount = 0
                    });
                }

                // ============================
                // 🔹 3. CALCULATE TOTAL
                // ============================
                decimal total = cartItems.Sum(x => x.Price * x.Quantity);

                // ============================
                // 🔹 4. APPLY COUPON FROM BODY
                // ============================
                decimal totalDiscount = 0;
                string appliedCoupon = null;

                if (!string.IsNullOrEmpty(model.CouponCode))
                {
                    string couponQuery = @"
            SELECT *
            FROM coupons
            WHERE code = @code
            AND is_active = TRUE
            AND start_date <= NOW()
            AND end_date >= NOW()";

                    using var cmd = new NpgsqlCommand(couponQuery, conn, transaction);
                    cmd.Parameters.AddWithValue("@code", model.CouponCode);

                    using var reader = await cmd.ExecuteReaderAsync();

                    if (await reader.ReadAsync())
                    {
                        var discountType = reader["discount_type"]?.ToString();
                        var discountValue = Convert.ToDecimal(reader["discount_value"]);
                        var minOrder = Convert.ToDecimal(reader["min_order_amount"]);

                        decimal? maxDiscount = reader["max_discount_amount"] == DBNull.Value
                            ? (decimal?)null
                            : Convert.ToDecimal(reader["max_discount_amount"]);

                        if (total >= minOrder)
                        {
                            if (discountType == "percentage")
                                totalDiscount = (total * discountValue) / 100;
                            else if (discountType == "fixed")
                                totalDiscount = discountValue;

                            if (maxDiscount.HasValue && totalDiscount > maxDiscount.Value)
                                totalDiscount = maxDiscount.Value;

                            appliedCoupon = model.CouponCode;
                        }
                        else
                        {
                            return new
                            {
                                success = false,
                                message = $"Minimum order should be {minOrder}"
                            };
                        }
                    }
                    else
                    {
                        return new
                        {
                            success = false,
                            message = "Invalid or expired coupon"
                        };
                    }

                    await reader.CloseAsync();
                }

                decimal finalAmount = total - totalDiscount;

                // ============================
                // 🔹 5. CREATE RAZORPAY ORDER
                // ============================
                string razorpayOrderId = null;

                if (model.PaymentMethod == "RAZORPAY")
                {
                    var client = new RazorpayClient(_key, _secret);

                    var options = new Dictionary<string, object>
            {
                { "amount", (int)(finalAmount * 100) },
                { "currency", "INR" },
                { "receipt", Guid.NewGuid().ToString() }
            };

                    var order = client.Order.Create(options);
                    razorpayOrderId = order["id"].ToString();
                }

                // ============================
                // 🔹 6. INSERT ORDER
                // ============================
                string insertOrder = @"
        INSERT INTO orders 
        (user_email, address_id, total_amount, discount_amount, final_amount,
         payment_method, payment_status, order_status, razorpay_order_id, coupon_code)
        VALUES (@email, @address, @total, @discount, @final,
                @method, @paymentStatus, @status, @razorpayId, @couponCode)
        RETURNING id";

                Guid orderId;

                using (var cmd = new NpgsqlCommand(insertOrder, conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@email", email);
                    cmd.Parameters.AddWithValue("@address", model.AddressId);
                    cmd.Parameters.AddWithValue("@total", total);
                    cmd.Parameters.AddWithValue("@discount", totalDiscount);
                    cmd.Parameters.AddWithValue("@final", finalAmount);
                    cmd.Parameters.AddWithValue("@method", model.PaymentMethod);
                    cmd.Parameters.AddWithValue("@paymentStatus",
                        model.PaymentMethod == "COD" ? "SUCCESS" : "PENDING");
                    cmd.Parameters.AddWithValue("@status", "PLACED");
                    cmd.Parameters.AddWithValue("@razorpayId", (object?)razorpayOrderId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@couponCode", (object?)appliedCoupon ?? DBNull.Value);

                    orderId = (Guid)await cmd.ExecuteScalarAsync();
                }

                // ============================
                // 🔹 7. INSERT ORDER ITEMS
                // ============================
                foreach (var item in cartItems)
                {
                    string itemQuery = @"
            INSERT INTO order_items 
            (order_id, product_id, quantity, price)
            VALUES (@oid, @pid, @qty, @price)";

                    using var cmd = new NpgsqlCommand(itemQuery, conn, transaction);
                    cmd.Parameters.AddWithValue("@oid", orderId);
                    cmd.Parameters.AddWithValue("@pid", item.ProductId);
                    cmd.Parameters.AddWithValue("@qty", item.Quantity);
                    cmd.Parameters.AddWithValue("@price", item.Price);

                    await cmd.ExecuteNonQueryAsync();
                }

                // ============================
                // 🔹 8. CLEAR CART
                // ============================
                string clearCart = @"
        DELETE FROM cart_items ci
        USING carts c
        WHERE ci.cart_id = c.id
        AND c.user_id = @userId";

                using (var cmd = new NpgsqlCommand(clearCart, conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@userId", userId);
                    await cmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();

                return new
                {
                    success = true,
                    orderId,
                    razorpayOrderId,
                    total,
                    discount = totalDiscount,
                    finalAmount,
                    couponCode = appliedCoupon
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                return new
                {
                    success = false,
                    message = ex.Message
                };
            }
        }
        public async Task<object> VerifyPayment(RazorpayVerifyModel model)
        {
            try
            {
                var attributes = new Dictionary<string, string>
        {
            { "razorpay_order_id", model.RazorpayOrderId },
            { "razorpay_payment_id", model.RazorpayPaymentId },
            { "razorpay_signature", model.RazorpaySignature }
        };

                Razorpay.Api.Utils.verifyPaymentSignature(attributes);

                using var conn = new NpgsqlConnection(DbConnection);
                await conn.OpenAsync();

                // 🔹 1. Update Orders Table
                string updateOrder = @"
        UPDATE orders 
        SET payment_status='SUCCESS',
            razorpay_payment_id=@paymentId,
            razorpay_signature=@signature
        WHERE razorpay_order_id=@orderId
        RETURNING id";

                Guid orderId;

                using (var cmd = new NpgsqlCommand(updateOrder, conn))
                {
                    cmd.Parameters.AddWithValue("@paymentId", model.RazorpayPaymentId);
                    cmd.Parameters.AddWithValue("@signature", model.RazorpaySignature);
                    cmd.Parameters.AddWithValue("@orderId", model.RazorpayOrderId);

                    orderId = (Guid)await cmd.ExecuteScalarAsync();
                }

                // 🔹 2. Update Payment Log
                string updateLog = @"
        UPDATE payment_logs
        SET razorpay_payment_id=@paymentId,
            razorpay_signature=@signature,
            status='SUCCESS'
        WHERE razorpay_order_id=@orderId";

                using (var cmd = new NpgsqlCommand(updateLog, conn))
                {
                    cmd.Parameters.AddWithValue("@paymentId", model.RazorpayPaymentId);
                    cmd.Parameters.AddWithValue("@signature", model.RazorpaySignature);
                    cmd.Parameters.AddWithValue("@orderId", model.RazorpayOrderId);

                    await cmd.ExecuteNonQueryAsync();
                }

                return new { success = true, message = "Payment verified & log updated" };
            }
            catch (Exception ex)
            {
                return new { success = false, message = ex.Message };
            }
        }

        public async Task<object> GetUserOrders(string email)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            string query = @"
    SELECT 
        o.id,
        o.total_amount,
        o.payment_status,
        o.order_status,
        o.created_at,

        a.id,
        a.full_name,
        a.phone_number,
        a.address_line1,
        a.address_line2,
        a.city,
        a.state,
        a.postal_code,
        a.address_type,

        oi.product_id,
        oi.quantity,
        oi.price,

        p.name,
        p.mainimage,
        p.description,
        p.price

    FROM orders o
    LEFT JOIN order_items oi ON o.id = oi.order_id
    LEFT JOIN address_details a ON o.address_id = a.id
    LEFT JOIN products p ON oi.product_id = p.id
    WHERE o.user_email = @email
    ORDER BY o.created_at DESC";

            var orderDict = new Dictionary<Guid, dynamic>();

            using (var cmd = new NpgsqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@email", email);

                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var orderId = reader.GetGuid(0);

                    if (!orderDict.ContainsKey(orderId))
                    {
                        orderDict[orderId] = new
                        {
                            orderId = orderId,
                            totalAmount = reader.GetDecimal(1),
                            paymentStatus = reader.GetString(2),
                            orderStatus = reader.GetString(3),
                            createdAt = reader.GetDateTime(4),

                            address = new
                            {
                                addressId = reader.IsDBNull(5) ? (Guid?)null : reader.GetGuid(5),
                                fullName = reader.IsDBNull(6) ? "" : reader.GetString(6),
                                phoneNumber = reader.IsDBNull(7) ? "" : reader.GetString(7),
                                addressLine1 = reader.IsDBNull(8) ? "" : reader.GetString(8),
                                addressLine2 = reader.IsDBNull(9) ? "" : reader.GetString(9),
                                city = reader.IsDBNull(10) ? "" : reader.GetString(10),
                                state = reader.IsDBNull(11) ? "" : reader.GetString(11),
                                postalCode = reader.IsDBNull(12) ? "" : reader.GetString(12),
                                addressType = reader.IsDBNull(13) ? "" : reader.GetString(13)
                            },

                            items = new List<object>()
                        };
                    }

                    // ✅ ITEMS WITH PRODUCT DETAILS
                    if (!reader.IsDBNull(14))
                    {
                        var item = new
                        {
                            productId = reader.GetGuid(14),
                            quantity = reader.GetInt32(15),
                            price = reader.GetDecimal(16),

                            productName = reader.IsDBNull(17) ? "" : reader.GetString(17),
                            productImage = reader.IsDBNull(18) ? "" : reader.GetString(18),
                            description = reader.IsDBNull(19) ? "" : reader.GetString(19),
                            productPrice = reader.IsDBNull(20) ? 0 : reader.GetDecimal(20)
                        };

                        ((List<object>)orderDict[orderId].items).Add(item);
                    }
                }
            }

            return new
            {
                success = true,
                data = orderDict.Values
            };
        }

        public async Task<object> CancelOrder(string email, Guid orderId)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            // 🔹 1. Get Order Details
            string getOrder = @"
    SELECT payment_method, payment_status, order_status
    FROM orders
    WHERE id = @orderId AND user_email = @email";

            string paymentMethod = "";
            string paymentStatus = "";
            string orderStatus = "";

            using (var cmd = new NpgsqlCommand(getOrder, conn))
            {
                cmd.Parameters.AddWithValue("@orderId", orderId);
                cmd.Parameters.AddWithValue("@email", email);

                using var reader = await cmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    return new { success = false, message = "Order not found" };
                }

                paymentMethod = reader.GetString(0);
                paymentStatus = reader.GetString(1);
                orderStatus = reader.GetString(2);
            }

            // 🔹 2. Validation
            if (orderStatus == "DELIVERED")
                return new { success = false, message = "Delivered order cannot be cancelled" };

            if (orderStatus == "CANCELLED")
                return new { success = false, message = "Order already cancelled" };

            // 🔹 3. Update Order Status
            string updateOrder = @"
    UPDATE orders
    SET order_status = 'CANCELLED'
    WHERE id = @orderId";

            using (var cmd = new NpgsqlCommand(updateOrder, conn))
            {
                cmd.Parameters.AddWithValue("@orderId", orderId);
                await cmd.ExecuteNonQueryAsync();
            }

            // 🔹 4. Handle Payment
            if (paymentMethod == "RAZORPAY" && paymentStatus == "SUCCESS")
            {
                // 👉 Future: call refund API
                // For now just mark as REFUND_PENDING

                string refundUpdate = @"
        UPDATE orders
        SET payment_status = 'REFUND_PENDING'
        WHERE id = @orderId";

                using var cmd = new NpgsqlCommand(refundUpdate, conn);
                cmd.Parameters.AddWithValue("@orderId", orderId);
                await cmd.ExecuteNonQueryAsync();
            }

            // 🔹 5. Update Payment Log
            string updateLog = @"
    UPDATE payment_logs
    SET status = 'CANCELLED'
    WHERE order_id = @orderId";

            using (var cmd = new NpgsqlCommand(updateLog, conn))
            {
                cmd.Parameters.AddWithValue("@orderId", orderId);
                await cmd.ExecuteNonQueryAsync();
            }

            return new
            {
                success = true,
                message = "Order cancelled successfully"
            };
        }

        public async Task<object> RequestExchange(string email, ExchangeRequestModel model)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            // 🔹 1. Validate Order
            string orderQuery = @"
    SELECT order_status 
    FROM orders 
    WHERE id = @orderId AND user_email = @email";

            string orderStatus = "";

            using (var cmd = new NpgsqlCommand(orderQuery, conn))
            {
                cmd.Parameters.AddWithValue("@orderId", model.OrderId);
                cmd.Parameters.AddWithValue("@email", email);

                var result = await cmd.ExecuteScalarAsync();

                if (result == null)
                    return new { success = false, message = "Order not found" };

                orderStatus = result.ToString();
            }

            // ❌ Only delivered orders allowed
            if (orderStatus != "DELIVERED")
                return new { success = false, message = "Only delivered orders can be exchanged" };

            // 🔹 2. Get Old Product
            string itemQuery = @"
    SELECT product_id 
    FROM order_items 
    WHERE id = @itemId AND order_id = @orderId";

            Guid oldProductId;

            using (var cmd = new NpgsqlCommand(itemQuery, conn))
            {
                cmd.Parameters.AddWithValue("@itemId", model.OrderItemId);
                cmd.Parameters.AddWithValue("@orderId", model.OrderId);

                var result = await cmd.ExecuteScalarAsync();

                if (result == null)
                    return new { success = false, message = "Order item not found" };

                oldProductId = (Guid)result;
            }

            // 🔹 3. Insert Exchange Request
            string insertQuery = @"
    INSERT INTO order_exchanges
    (order_id, order_item_id, user_email, old_product_id, new_product_id, reason, status)
    VALUES (@orderId, @itemId, @email, @oldProduct, @newProduct, @reason, 'REQUESTED')";

            using (var cmd = new NpgsqlCommand(insertQuery, conn))
            {
                cmd.Parameters.AddWithValue("@orderId", model.OrderId);
                cmd.Parameters.AddWithValue("@itemId", model.OrderItemId);
                cmd.Parameters.AddWithValue("@email", email);
                cmd.Parameters.AddWithValue("@oldProduct", oldProductId);
                cmd.Parameters.AddWithValue("@newProduct", model.NewProductId);
                cmd.Parameters.AddWithValue("@reason", model.Reason);

                await cmd.ExecuteNonQueryAsync();
            }

            return new
            {
                success = true,
                message = "Exchange request submitted"
            };
        }

        public async Task<object> GetExchangeRequests(string email, bool isAdmin)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            string query = @"
    SELECT 
        e.id,
        e.order_id,
        e.order_item_id,
        e.old_product_id,
        e.new_product_id,
        e.reason,
        e.status,
        e.created_at,

        o.order_status,

        p1.name AS old_product_name,
        p2.name AS new_product_name

    FROM order_exchanges e
    LEFT JOIN orders o ON e.order_id = o.id
    LEFT JOIN products p1 ON e.old_product_id = p1.id
    LEFT JOIN products p2 ON e.new_product_id = p2.id
    " + (isAdmin ? "" : "WHERE e.user_email = @email") + @"
    ORDER BY e.created_at DESC";

            var list = new List<object>();

            using (var cmd = new NpgsqlCommand(query, conn))
            {
                if (!isAdmin)
                    cmd.Parameters.AddWithValue("@email", email);

                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var item = new
                    {
                        exchangeId = reader.GetGuid(0),
                        orderId = reader.GetGuid(1),
                        orderItemId = reader.GetGuid(2),

                        oldProductId = reader.GetGuid(3),
                        newProductId = reader.GetGuid(4),

                        oldProductName = reader.IsDBNull(9) ? "" : reader.GetString(9),
                        newProductName = reader.IsDBNull(10) ? "" : reader.GetString(10),

                        reason = reader.IsDBNull(5) ? "" : reader.GetString(5),
                        status = reader.GetString(6),
                        createdAt = reader.GetDateTime(7),

                        orderStatus = reader.IsDBNull(8) ? "" : reader.GetString(8)
                    };

                    list.Add(item);
                }
            }

            return new
            {
                success = true,
                data = list
            };
        }

        public async Task<object> UpdateExchangeStatus(UpdateExchangeStatusModel model)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            // 🔹 1. Validate Status
            if (model.Status != "APPROVED" && model.Status != "REJECTED")
            {
                return new { success = false, message = "Invalid status" };
            }

            // 🔹 2. Get Existing Exchange Request
            string getQuery = @"
    SELECT status 
    FROM order_exchanges 
    WHERE id = @id";

            string currentStatus = "";

            using (var cmd = new NpgsqlCommand(getQuery, conn))
            {
                cmd.Parameters.AddWithValue("@id", model.ExchangeId);

                var result = await cmd.ExecuteScalarAsync();

                if (result == null)
                    return new { success = false, message = "Exchange request not found" };

                currentStatus = result.ToString();
            }

            // ❌ Prevent duplicate updates
            if (currentStatus == "APPROVED" || currentStatus == "REJECTED")
            {
                return new
                {
                    success = false,
                    message = $"Already {currentStatus}"
                };
            }

            // 🔹 3. Update Exchange Status
            string updateQuery = @"
    UPDATE order_exchanges
    SET status = @status
    WHERE id = @id";

            using (var cmd = new NpgsqlCommand(updateQuery, conn))
            {
                cmd.Parameters.AddWithValue("@status", model.Status);
                cmd.Parameters.AddWithValue("@id", model.ExchangeId);

                await cmd.ExecuteNonQueryAsync();
            }

            return new
            {
                success = true,
                message = $"Exchange {model.Status} successfully"
            };
        }

        public async Task<object> SchedulePickup(PickupRequestModel model)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            // 🔹 1. Check Exchange Status
            string checkQuery = @"
    SELECT order_id, status 
    FROM order_exchanges 
    WHERE id = @exchangeId";

            Guid orderId;
            string status;

            using (var cmd = new NpgsqlCommand(checkQuery, conn))
            {
                cmd.Parameters.AddWithValue("@exchangeId", model.ExchangeId);

                using var reader = await cmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                    return new { success = false, message = "Exchange not found" };

                orderId = reader.GetGuid(0);
                status = reader.GetString(1);
            }

            // ❌ Only APPROVED allowed
            if (status != "APPROVED")
                return new { success = false, message = "Pickup allowed only for approved exchange" };

            // 🔹 2. Prevent duplicate pickup
            string checkPickup = "SELECT COUNT(*) FROM exchange_pickups WHERE exchange_id=@id";

            using (var cmd = new NpgsqlCommand(checkPickup, conn))
            {
                cmd.Parameters.AddWithValue("@id", model.ExchangeId);

                var count = (long)await cmd.ExecuteScalarAsync();

                if (count > 0)
                    return new { success = false, message = "Pickup already scheduled" };
            }

            // 🔹 3. Insert Pickup
            string insertQuery = @"
    INSERT INTO exchange_pickups
    (exchange_id, order_id, pickup_address, pickup_date, status)
    VALUES (@exchangeId, @orderId, @address, @date, 'SCHEDULED')";

            using (var cmd = new NpgsqlCommand(insertQuery, conn))
            {
                cmd.Parameters.AddWithValue("@exchangeId", model.ExchangeId);
                cmd.Parameters.AddWithValue("@orderId", orderId);
                cmd.Parameters.AddWithValue("@address", model.PickupAddress);
                cmd.Parameters.AddWithValue("@date", model.PickupDate);

                await cmd.ExecuteNonQueryAsync();
            }

            return new
            {
                success = true,
                message = "Pickup scheduled successfully"
            };
        }

        public async Task<object> CompleteExchange(Guid exchangeId)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            using var transaction = await conn.BeginTransactionAsync();

            try
            {
                // 🔹 1. Get Exchange Details
                string exchangeQuery = @"
        SELECT order_id, order_item_id, new_product_id, status
        FROM order_exchanges
        WHERE id = @exchangeId";

                Guid orderId;
                Guid orderItemId;
                Guid newProductId;
                string status;

                using (var cmd = new NpgsqlCommand(exchangeQuery, conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@exchangeId", exchangeId);

                    using var reader = await cmd.ExecuteReaderAsync();

                    if (!await reader.ReadAsync())
                        return new { success = false, message = "Exchange not found" };

                    orderId = reader.GetGuid(0);
                    orderItemId = reader.GetGuid(1);
                    newProductId = reader.GetGuid(2);
                    status = reader.GetString(3);
                }

                // ❌ Only APPROVED allowed
                if (status != "APPROVED")
                    return new { success = false, message = "Exchange not approved" };

                // 🔹 2. Check Pickup Status
                string pickupQuery = @"
        SELECT status FROM exchange_pickups 
        WHERE exchange_id = @exchangeId";

                string pickupStatus = "";

                using (var cmd = new NpgsqlCommand(pickupQuery, conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@exchangeId", exchangeId);

                    var result = await cmd.ExecuteScalarAsync();

                    if (result == null)
                        return new { success = false, message = "Pickup not scheduled" };

                    pickupStatus = result.ToString();
                }

                if (pickupStatus != "PICKED")
                    return new { success = false, message = "Pickup not completed yet" };

                // 🔹 3. Get Old Order Info
                string orderQuery = @"
        SELECT user_email, address_id
        FROM orders
        WHERE id = @orderId";

                string userEmail;
                Guid addressId;

                using (var cmd = new NpgsqlCommand(orderQuery, conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@orderId", orderId);

                    using var reader = await cmd.ExecuteReaderAsync();

                    if (!await reader.ReadAsync())
                        return new { success = false, message = "Order not found" };

                    userEmail = reader.GetString(0);
                    addressId = reader.GetGuid(1);
                }

                // 🔹 4. Get Price of New Product
                decimal price;

                using (var cmd = new NpgsqlCommand("SELECT price FROM products WHERE id=@pid", conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@pid", newProductId);
                    var result = await cmd.ExecuteScalarAsync();

                    if (result == null)
                        return new { success = false, message = "Product not found" };

                    price = (decimal)result;
                }

                // 🔹 5. Create New Order
                string insertOrder = @"
        INSERT INTO orders
        (user_email, address_id, total_amount, payment_method, payment_status, order_status)
        VALUES (@email, @address, @amount, 'EXCHANGE', 'SUCCESS', 'PLACED')
        RETURNING id";

                Guid newOrderId;

                using (var cmd = new NpgsqlCommand(insertOrder, conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@email", userEmail);
                    cmd.Parameters.AddWithValue("@address", addressId);
                    cmd.Parameters.AddWithValue("@amount", price);

                    newOrderId = (Guid)await cmd.ExecuteScalarAsync();
                }

                // 🔹 6. Insert Order Item
                string insertItem = @"
        INSERT INTO order_items (order_id, product_id, quantity, price)
        VALUES (@oid, @pid, 1, @price)";

                using (var cmd = new NpgsqlCommand(insertItem, conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@oid", newOrderId);
                    cmd.Parameters.AddWithValue("@pid", newProductId);
                    cmd.Parameters.AddWithValue("@price", price);

                    await cmd.ExecuteNonQueryAsync();
                }

                // 🔹 7. Mark Exchange Completed
                string updateExchange = @"
        UPDATE order_exchanges
        SET status = 'COMPLETED'
        WHERE id = @exchangeId";

                using (var cmd = new NpgsqlCommand(updateExchange, conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@exchangeId", exchangeId);
                    await cmd.ExecuteNonQueryAsync();
                }

                // 🔹 8. Update Pickup Status
                string updatePickup = @"
        UPDATE exchange_pickups
        SET status = 'COMPLETED'
        WHERE exchange_id = @exchangeId";

                using (var cmd = new NpgsqlCommand(updatePickup, conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@exchangeId", exchangeId);
                    await cmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();

                return new
                {
                    success = true,
                    message = "Exchange completed & replacement order created",
                    newOrderId = newOrderId
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                return new
                {
                    success = false,
                    message = ex.Message
                };
            }
        }

        public async Task<object> UpdatePickupStatus(Guid exchangeId, string status)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            string query = @"
    UPDATE exchange_pickups
    SET status = @status
    WHERE exchange_id = @id";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@status", status);
            cmd.Parameters.AddWithValue("@id", exchangeId);

            await cmd.ExecuteNonQueryAsync();

            return new
            {
                success = true,
                message = "Pickup status updated"
            };
        }

        public async Task<object> GetMyOrders(string email)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            // ============================
            // 🔹 1. GET USER ID
            // ============================
            string userQuery = @"SELECT ""Id"" FROM ""AspNetUsers"" WHERE ""Email""=@Email LIMIT 1";

            Guid userId;

            using (var cmd = new NpgsqlCommand(userQuery, conn))
            {
                cmd.Parameters.AddWithValue("@Email", email);
                var result = await cmd.ExecuteScalarAsync();

                if (result == null)
                    return new { success = false, message = "User not found" };

                userId = Guid.Parse(result.ToString());
            }

            // ============================
            // 🔹 2. GET ORDERS + ITEMS + ADDRESS
            // ============================
            string query = @"
SELECT 
    o.id AS order_id,
    o.total_amount,
    o.discount_amount,
    o.final_amount,
    o.payment_method,
    o.payment_status,
    o.order_status,
    o.created_at,

    -- Address
    a.id AS address_id,
    a.full_name,
    a.phone_number,
    a.address_line1,
    a.address_line2,
    a.city,
    a.state,
    a.postal_code,

    -- Items
    oi.product_id,
    oi.quantity,
    oi.price,
    oi.discount,

    p.name,
    p.mainimage

FROM orders o
LEFT JOIN address_details a ON a.id = o.address_id
LEFT JOIN order_items oi ON oi.order_id = o.id
LEFT JOIN products p ON p.id = oi.product_id

WHERE o.user_email = @email
AND o.order_status != 'DELIVERED'

ORDER BY o.created_at DESC";

            using var cmd2 = new NpgsqlCommand(query, conn);
            cmd2.Parameters.AddWithValue("@email", email);

            using var reader = await cmd2.ExecuteReaderAsync();

            var orderDict = new Dictionary<Guid, dynamic>();

            while (await reader.ReadAsync())
            {
                var orderId = reader.GetGuid(0);

                if (!orderDict.ContainsKey(orderId))
                {
                    orderDict[orderId] = new
                    {
                        OrderId = orderId,
                        TotalAmount = reader.GetDecimal(1),
                        Discount = reader.IsDBNull(2) ? 0 : reader.GetDecimal(2),
                        FinalAmount = reader.GetDecimal(3),
                        PaymentMethod = reader.GetString(4),
                        PaymentStatus = reader.GetString(5),
                        OrderStatus = reader.GetString(6),
                        CreatedAt = reader.GetDateTime(7),

                        Address = new
                        {
                            AddressId = reader.IsDBNull(8) ? Guid.Empty : reader.GetGuid(8),
                            Name = reader.IsDBNull(9) ? null : reader.GetString(9),
                            Phone = reader.IsDBNull(10) ? null : reader.GetString(10),
                            AddressLine1 = reader.IsDBNull(11) ? null : reader.GetString(11),
                            AddressLine2 = reader.IsDBNull(12) ? null : reader.GetString(12),
                            City = reader.IsDBNull(13) ? null : reader.GetString(13),
                            State = reader.IsDBNull(14) ? null : reader.GetString(14),
                            Pincode = reader.IsDBNull(15) ? null : reader.GetString(15)
                        },

                        Items = new List<object>()
                    };
                }

                // 🔹 Add items
                if (!reader.IsDBNull(16))
                {
                    var item = new
                    {
                        ProductId = reader.GetGuid(16),
                        Quantity = reader.GetInt32(17),
                        Price = reader.GetDecimal(18),
                        Discount = reader.IsDBNull(19) ? 0 : reader.GetDecimal(19),
                        ProductName = reader.IsDBNull(20) ? null : reader.GetString(20),
                        ProductImage = reader.IsDBNull(21) ? null : reader.GetString(21)
                    };

                    ((List<object>)orderDict[orderId].Items).Add(item);
                }
            }

            return new
            {
                success = true,
                count = orderDict.Count,
                orders = orderDict.Values
            };
        }

        public async Task<object> UpdateOrderStatus(UpdateOrderStatusModel model)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            var validStatuses = new List<string>
    {
        "PLACED",
        "CONFIRMED",
        "SHIPPED",
        "OUT_FOR_DELIVERY",
        "DELIVERED",
        "CANCELLED"
    };

            if (!validStatuses.Contains(model.Status))
            {
                return new { success = false, message = "Invalid order status" };
            }

            // 🔹 Get current status
            string getQuery = @"SELECT order_status FROM orders WHERE id=@id";

            string currentStatus;

            using (var cmd = new NpgsqlCommand(getQuery, conn))
            {
                cmd.Parameters.AddWithValue("@id", model.OrderId);

                var result = await cmd.ExecuteScalarAsync();

                if (result == null)
                    return new { success = false, message = "Order not found" };

                currentStatus = result.ToString();
            }

            // ❌ Prevent update if completed
            if (currentStatus == "DELIVERED" || currentStatus == "CANCELLED")
            {
                return new
                {
                    success = false,
                    message = $"Order already {currentStatus}"
                };
            }

            // 🔹 Update order
            string updateQuery = @"
        UPDATE orders
        SET order_status = @status
        WHERE id = @id";

            using (var cmd = new NpgsqlCommand(updateQuery, conn))
            {
                cmd.Parameters.AddWithValue("@status", model.Status);
                cmd.Parameters.AddWithValue("@id", model.OrderId);

                await cmd.ExecuteNonQueryAsync();
            }

            // 🔥 SAVE EMAIL HERE
            string historyQuery = @"
        INSERT INTO order_status_history(order_id, status, updated_by_email)
        VALUES(@orderId, @status, @email)";

            using (var cmd = new NpgsqlCommand(historyQuery, conn))
            {
                cmd.Parameters.AddWithValue("@orderId", model.OrderId);
                cmd.Parameters.AddWithValue("@status", model.Status);
                cmd.Parameters.AddWithValue("@email", model.UpdatedByEmail);

                await cmd.ExecuteNonQueryAsync();
            }

            return new
            {
                success = true,
                message = $"Order status updated to {model.Status}"
            };
        }

        public async Task<object> TrackOrder(Guid orderId)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            // 🔹 ORDER DETAILS
            string orderQuery = @"
        SELECT 
            id,
            order_status,
            payment_status,
            created_at
        FROM orders
        WHERE id = @id";

            using var cmd = new NpgsqlCommand(orderQuery, conn);
            cmd.Parameters.AddWithValue("@id", orderId);

            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                return new { success = false, message = "Order not found" };
            }

            var order = new
            {
                OrderId = reader.GetGuid(0),
                Status = reader.GetString(1),
                PaymentStatus = reader.GetString(2),
                CreatedAt = reader.GetDateTime(3)
            };

            await reader.CloseAsync();

            // 🔹 STATUS HISTORY
            string historyQuery = @"
        SELECT status, created_at
        FROM order_status_history
        WHERE order_id = @id
        ORDER BY created_at ASC";

            var history = new List<object>();

            using var cmd2 = new NpgsqlCommand(historyQuery, conn);
            cmd2.Parameters.AddWithValue("@id", orderId);

            using var reader2 = await cmd2.ExecuteReaderAsync();

            while (await reader2.ReadAsync())
            {
                history.Add(new
                {
                    Status = reader2.GetString(0),
                    Time = reader2.GetDateTime(1)
                });
            }

            return new
            {
                success = true,
                order,
                tracking = history
            };
        }

        public async Task<object> GetOrderDetails(string email, Guid orderId)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            OrderDetailsResponse order = null;

            // ============================
            // 🔹 1. ORDER + ADDRESS + PAYMENT
            // ============================
            using (var cmd = new NpgsqlCommand(@"
        SELECT 
            o.id, o.total_amount, o.discount_amount, o.final_amount,
            o.payment_method, o.payment_status, o.order_status,
            o.coupon_code, o.created_at,
            o.razorpay_order_id, o.razorpay_payment_id, o.razorpay_signature,
            a.id, a.full_name, a.phone_number,
            a.address_line1, a.address_line2, a.city, a.state, a.postal_code
        FROM orders o
        LEFT JOIN address_details a ON a.id = o.address_id
        WHERE o.id = @orderId", conn))   // ✅ removed email filter
            {
                cmd.Parameters.AddWithValue("@orderId", orderId);

                using var reader = await cmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                    return new { success = false, message = "Order not found" };

                order = new OrderDetailsResponse
                {
                    OrderId = reader.GetGuid(0),
                    TotalAmount = reader.GetDecimal(1),
                    Discount = reader.IsDBNull(2) ? 0 : reader.GetDecimal(2),
                    FinalAmount = reader.GetDecimal(3),

                    PaymentMethod = reader.GetString(4),
                    PaymentStatus = reader.GetString(5),
                    OrderStatus = reader.GetString(6),

                    CouponCode = reader.IsDBNull(7) ? null : reader.GetString(7),
                    CreatedAt = reader.GetDateTime(8),

                    Payment = new PaymentModel
                    {
                        RazorpayOrderId = reader.IsDBNull(9) ? null : reader.GetString(9),
                        RazorpayPaymentId = reader.IsDBNull(10) ? null : reader.GetString(10),
                        RazorpaySignature = reader.IsDBNull(11) ? null : reader.GetString(11),
                        Status = reader.GetString(5),
                        Method = reader.GetString(4)
                    },

                    Address = new AddressModel
                    {
                        AddressId = reader.IsDBNull(12) ? Guid.Empty : reader.GetGuid(12),
                        Name = reader.IsDBNull(13) ? null : reader.GetString(13),
                        Phone = reader.IsDBNull(14) ? null : reader.GetString(14),
                        AddressLine1 = reader.IsDBNull(15) ? null : reader.GetString(15),
                        AddressLine2 = reader.IsDBNull(16) ? null : reader.GetString(16),
                        City = reader.IsDBNull(17) ? null : reader.GetString(17),
                        State = reader.IsDBNull(18) ? null : reader.GetString(18),
                        Pincode = reader.IsDBNull(19) ? null : reader.GetString(19)
                    }
                };
            }

            // ============================
            // 🔹 2. ORDER ITEMS
            // ============================
            using (var cmd2 = new NpgsqlCommand(@"
        SELECT oi.product_id, oi.quantity, oi.price, oi.discount,
               p.name, p.mainimage, p.description, p.price
        FROM order_items oi
        LEFT JOIN products p ON p.id = oi.product_id
        WHERE oi.order_id = @orderId", conn))
            {
                cmd2.Parameters.AddWithValue("@orderId", orderId);

                using var reader2 = await cmd2.ExecuteReaderAsync();

                while (await reader2.ReadAsync())
                {
                    order.Items.Add(new OrderItemModelResponse
                    {
                        ProductId = reader2.GetGuid(0),
                        Quantity = reader2.GetInt32(1),
                        Price = reader2.GetDecimal(2),
                        Discount = reader2.IsDBNull(3) ? 0 : reader2.GetDecimal(3),

                        ProductName = reader2.IsDBNull(4) ? null : reader2.GetString(4),
                        ProductImage = reader2.IsDBNull(5) ? null : reader2.GetString(5),
                        Description = reader2.IsDBNull(6) ? null : reader2.GetString(6),
                        ProductPrice = reader2.IsDBNull(7) ? 0 : reader2.GetDecimal(7)
                    });
                }
            }

            // ============================
            // 🔹 FINAL RESPONSE
            // ============================
            return new
            {
                success = true,
                order
            };
        }
    }
}