using elemechWisetrack.Models;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Razorpay.Api;

namespace elemechWisetrack.DataBaseLayer
{
    public interface IDataBaseLayer_Orders
    {
        Task<object> GetCheckoutDetails(string email);
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
    }
    public partial interface IDataBaseLayer : IDataBaseLayer_Orders
    {
    }

    public partial class DataBaseLayer
    {
        //public async Task<object> GetCheckoutDetails(string email)
        //{
        //    using var conn = new NpgsqlConnection(DbConnection);
        //    await conn.OpenAsync();

        //    // 🔹 1. Get User ID
        //    string getUserQuery = @"SELECT ""Id"" FROM ""AspNetUsers"" WHERE ""Email""=@Email LIMIT 1";
        //    Guid userId;

        //    using (var cmd = new NpgsqlCommand(getUserQuery, conn))
        //    {
        //        cmd.Parameters.AddWithValue("@Email", email);
        //        var result = await cmd.ExecuteScalarAsync();
        //        if (result == null)
        //            return new { success = false, message = "User not found" };
        //        userId = Guid.Parse(result.ToString());
        //    }

        //    // 🔹 2. Get Cart Items with Product Info
        //    List<CartProductDetail> cartItems = new();
        //    string cartQuery = @"
        //SELECT ci.product_id, ci.quantity, p.name, p.price, p.mainimage
        //FROM cart_items ci
        //JOIN carts c ON c.id = ci.cart_id
        //JOIN products p ON p.id = ci.product_id
        //WHERE c.user_id = @userId";

        //    using (var cmd = new NpgsqlCommand(cartQuery, conn))
        //    {
        //        cmd.Parameters.AddWithValue("@userId", userId);
        //        using var reader = await cmd.ExecuteReaderAsync();
        //        while (await reader.ReadAsync())
        //        {
        //            cartItems.Add(new CartProductDetail
        //            {
        //                ProductId = reader.GetGuid(0),
        //                Quantity = reader.GetInt32(1),
        //                ProductName = reader.GetString(2),
        //                Price = reader.GetDecimal(3),
        //                Image = reader.IsDBNull(4) ? null : reader.GetString(4)
        //            });
        //        }
        //    }

        //    // 🔹 3. Get User Addresses
        ////    List<UserAddressDetail> addresses = new();
        ////    string addressQuery = @"
        ////SELECT id, full_name, phone_number, address_line1, address_line2, city, state, postal_code
        ////FROM address_details
        ////WHERE user_id = @userId";

        ////    using (var cmd = new NpgsqlCommand(addressQuery, conn))
        ////    {
        ////        cmd.Parameters.AddWithValue("@userId", userId);
        ////        using var reader = await cmd.ExecuteReaderAsync();
        ////        while (await reader.ReadAsync())
        ////        {
        ////            addresses.Add(new UserAddressDetail
        ////            {
        ////                AddressId = reader.GetGuid(0),
        ////                Name = reader.GetString(1),
        ////                Phone = reader.GetString(2),
        ////                AddressLine1 = reader.GetString(3),
        ////                AddressLine2 = reader.GetString(4),
        ////                City = reader.GetString(5),
        ////                State = reader.GetString(6),
        ////                Pincode = reader.GetString(7)
        ////            });
        ////        }
        ////    }

        //    decimal totalAmount = cartItems.Sum(x => x.Price * x.Quantity);

        //    return new CheckoutDetailsResponse
        //    {
        //        UserId = userId,
        //        Email = email,
        //        CartItems = cartItems,
        //        //Addresses = addresses,
        //        TotalAmount = totalAmount
        //    };
        //}

        // ✅ CREATE ORDER

        public async Task<object> GetCheckoutDetails(string email)
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
                        Image = reader.IsDBNull(4) ? null : reader.GetString(4),
                        Discount = 0 // ✅ ADD THIS PROPERTY IN MODEL
                    });
                }
            }

            // ============================
            // 🔹 3. FETCH COUPONS
            // ============================
            decimal totalAmount = cartItems.Sum(x => x.Price * x.Quantity);
            decimal totalDiscount = 0;

            if (cartItems.Count > 0)
            {
                var productIds = cartItems.Select(x => x.ProductId).ToArray();

                string couponQuery = @"
        SELECT cu.product_id,
               c.discount_type,
               c.discount_value,
               c.min_order_amount,
               c.max_discount_amount
        FROM coupon_usage cu
        JOIN coupons c ON c.id = cu.coupon_id
        WHERE cu.user_email = @user_email
        AND cu.product_id = ANY(@product_ids)
        AND c.is_active = TRUE
        AND c.start_date <= NOW()
        AND c.end_date >= NOW()";

                using var couponCmd = new NpgsqlCommand(couponQuery, conn);
                couponCmd.Parameters.AddWithValue("@user_email", email);
                couponCmd.Parameters.AddWithValue("@product_ids", productIds);

                using var couponReader = await couponCmd.ExecuteReaderAsync();

                var couponMap = new Dictionary<Guid, List<CouponModel>>();

                while (await couponReader.ReadAsync())
                {
                    var productId = couponReader.GetGuid(0);

                    var coupon = new CouponModel
                    {
                        DiscountType = couponReader["discount_type"]?.ToString(),
                        DiscountValue = Convert.ToDecimal(couponReader["discount_value"]),
                        MinOrderAmount = Convert.ToDecimal(couponReader["min_order_amount"]),
                        MaxDiscountAmount = couponReader["max_discount_amount"] == DBNull.Value
                            ? (decimal?)null
                            : Convert.ToDecimal(couponReader["max_discount_amount"])
                    };

                    if (!couponMap.ContainsKey(productId))
                        couponMap[productId] = new List<CouponModel>();

                    couponMap[productId].Add(coupon);
                }

                await couponReader.CloseAsync();

                // ============================
                // 🔹 4. APPLY BEST COUPON PER PRODUCT
                // ============================
                foreach (var item in cartItems)
                {
                    decimal itemTotal = item.Price * item.Quantity;

                    if (!couponMap.ContainsKey(item.ProductId))
                        continue;

                    decimal bestDiscount = 0;

                    foreach (var coupon in couponMap[item.ProductId])
                    {
                        if (itemTotal < coupon.MinOrderAmount)
                            continue;

                        decimal tempDiscount = 0;

                        if (coupon.DiscountType == "percentage")
                        {
                            tempDiscount = (itemTotal * coupon.DiscountValue) / 100;
                        }
                        else if (coupon.DiscountType == "fixed")
                        {
                            tempDiscount = coupon.DiscountValue;
                        }

                        if (coupon.MaxDiscountAmount.HasValue &&
                            tempDiscount > coupon.MaxDiscountAmount.Value)
                        {
                            tempDiscount = coupon.MaxDiscountAmount.Value;
                        }

                        if (tempDiscount > bestDiscount)
                            bestDiscount = tempDiscount;
                    }

                    item.Discount = bestDiscount;
                    totalDiscount += bestDiscount;
                }
            }

            decimal finalAmount = totalAmount - totalDiscount;

            // ============================
            // 🔹 RESPONSE
            // ============================
            return new
            {
                UserId = userId,
                Email = email,
                CartItems = cartItems,
                TotalAmount = totalAmount,
                Discount = totalDiscount,
                FinalAmount = finalAmount
            };
        }
        //public async Task<object> CreateOrder(string email, CreateOrderModel model)
        //{
        //    using var conn = new NpgsqlConnection(DbConnection);
        //    await conn.OpenAsync();

        //    using var transaction = await conn.BeginTransactionAsync();

        //    try
        //    {
        //        // 🔹 1. GET USER ID
        //        string getUserQuery = @"SELECT ""Id"" FROM ""AspNetUsers"" WHERE ""Email""=@Email LIMIT 1";

        //        Guid userId;

        //        using (var cmd = new NpgsqlCommand(getUserQuery, conn, transaction))
        //        {
        //            cmd.Parameters.AddWithValue("@Email", email);
        //            var result = await cmd.ExecuteScalarAsync();

        //            if (result == null)
        //                return new { success = false, message = "User not found" };

        //            userId = Guid.Parse(result.ToString());
        //        }

        //        // 🔹 2. VALIDATE ITEMS
        //        if (model.Items == null || !model.Items.Any())
        //            return new { success = false, message = "No items provided" };

        //        var cartItems = new List<(Guid productId, int qty, decimal price)>();

        //        foreach (var item in model.Items)
        //        {
        //            string productQuery = "SELECT price FROM products WHERE id=@pid";

        //            using var cmd = new NpgsqlCommand(productQuery, conn, transaction);
        //            cmd.Parameters.AddWithValue("@pid", item.ProductId);

        //            var result = await cmd.ExecuteScalarAsync();

        //            if (result == null)
        //                return new { success = false, message = "Invalid product" };

        //            decimal price = (decimal)result;

        //            if (item.Quantity <= 0)
        //                return new { success = false, message = "Invalid quantity" };

        //            cartItems.Add((item.ProductId, item.Quantity, price));
        //        }

        //        // 🔹 3. CALCULATE TOTAL
        //        decimal total = cartItems.Sum(x => x.qty * x.price);

        //        string razorpayOrderId = null;

        //        // 🔹 4. CREATE RAZORPAY ORDER
        //        if (model.PaymentMethod == "RAZORPAY")
        //        {
        //            var client = new RazorpayClient(_key, _secret);

        //            var options = new Dictionary<string, object>
        //    {
        //        { "amount", (int)(total * 100) },
        //        { "currency", "INR" },
        //        { "receipt", Guid.NewGuid().ToString() }
        //    };

        //            var order = client.Order.Create(options);
        //            razorpayOrderId = order["id"].ToString();
        //        }

        //        // 🔹 5. INSERT ORDER
        //        string insertOrder = @"
        //INSERT INTO orders 
        //(user_email, address_id, total_amount, payment_method, payment_status, order_status, razorpay_order_id)
        //VALUES (@email, @address, @total, @method, @paymentStatus, @status, @razorpayId)
        //RETURNING id";

        //        Guid orderId;

        //        using (var cmd = new NpgsqlCommand(insertOrder, conn, transaction))
        //        {
        //            cmd.Parameters.AddWithValue("@email", email);
        //            cmd.Parameters.AddWithValue("@address", model.AddressId);
        //            cmd.Parameters.AddWithValue("@total", total);
        //            cmd.Parameters.AddWithValue("@method", model.PaymentMethod);
        //            cmd.Parameters.AddWithValue("@paymentStatus",
        //                model.PaymentMethod == "COD" ? "SUCCESS" : "PENDING");
        //            cmd.Parameters.AddWithValue("@status", "PLACED");
        //            cmd.Parameters.AddWithValue("@razorpayId", (object?)razorpayOrderId ?? DBNull.Value);

        //            orderId = (Guid)await cmd.ExecuteScalarAsync();
        //        }

        //        // 🔹 6. INSERT ORDER ITEMS
        //        foreach (var item in cartItems)
        //        {
        //            string itemQuery = @"
        //    INSERT INTO order_items (order_id, product_id, quantity, price)
        //    VALUES (@oid, @pid, @qty, @price)";

        //            using var cmd = new NpgsqlCommand(itemQuery, conn, transaction);
        //            cmd.Parameters.AddWithValue("@oid", orderId);
        //            cmd.Parameters.AddWithValue("@pid", item.productId);
        //            cmd.Parameters.AddWithValue("@qty", item.qty);
        //            cmd.Parameters.AddWithValue("@price", item.price);

        //            await cmd.ExecuteNonQueryAsync();
        //        }

        //        // 🔹 7. CLEAR CART (OPTIONAL BUT RECOMMENDED)
        //        string clearCart = @"
        //DELETE FROM cart_items ci
        //USING carts c
        //WHERE ci.cart_id = c.id
        //AND c.user_id = @userId";

        //        using (var cmd = new NpgsqlCommand(clearCart, conn, transaction))
        //        {
        //            cmd.Parameters.AddWithValue("@userId", userId);
        //            await cmd.ExecuteNonQueryAsync();
        //        }

        //        // 🔹 8. COMMIT
        //        await transaction.CommitAsync();

        //        return new
        //        {
        //            success = true,
        //            orderId,
        //            razorpayOrderId,
        //            amount = total
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        await transaction.RollbackAsync();

        //        return new
        //        {
        //            success = false,
        //            message = ex.Message
        //        };
        //    }
        //}

        // ✅ VERIFY PAYMENT

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
                // 🔹 3. FETCH COUPONS
                // ============================
                var productIds = cartItems.Select(x => x.ProductId).ToArray();

                string couponQuery = @"
        SELECT cu.product_id,
               c.discount_type,
               c.discount_value,
               c.min_order_amount,
               c.max_discount_amount
        FROM coupon_usage cu
        JOIN coupons c ON c.id = cu.coupon_id
        WHERE cu.user_email = @user_email
        AND cu.product_id = ANY(@product_ids)
        AND c.is_active = TRUE
        AND c.start_date <= NOW()
        AND c.end_date >= NOW()";

                var couponMap = new Dictionary<Guid, List<CouponModel>>();

                using (var cmd = new NpgsqlCommand(couponQuery, conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@user_email", email);
                    cmd.Parameters.AddWithValue("@product_ids", productIds);

                    using var reader = await cmd.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        var productId = reader.GetGuid(0);

                        var coupon = new CouponModel
                        {
                            DiscountType = reader["discount_type"]?.ToString(),
                            DiscountValue = Convert.ToDecimal(reader["discount_value"]),
                            MinOrderAmount = Convert.ToDecimal(reader["min_order_amount"]),
                            MaxDiscountAmount = reader["max_discount_amount"] == DBNull.Value
                                ? (decimal?)null
                                : Convert.ToDecimal(reader["max_discount_amount"])
                        };

                        if (!couponMap.ContainsKey(productId))
                            couponMap[productId] = new List<CouponModel>();

                        couponMap[productId].Add(coupon);
                    }
                }

                // ============================
                // 🔹 4. APPLY COUPON PER PRODUCT
                // ============================
                decimal total = 0;
                decimal totalDiscount = 0;

                foreach (var item in cartItems)
                {
                    decimal itemTotal = item.Price * item.Quantity;
                    decimal bestDiscount = 0;

                    if (couponMap.ContainsKey(item.ProductId))
                    {
                        foreach (var coupon in couponMap[item.ProductId])
                        {
                            if (itemTotal < coupon.MinOrderAmount)
                                continue;

                            decimal tempDiscount = 0;

                            if (coupon.DiscountType == "percentage")
                                tempDiscount = (itemTotal * coupon.DiscountValue) / 100;
                            else if (coupon.DiscountType == "fixed")
                                tempDiscount = coupon.DiscountValue;

                            if (coupon.MaxDiscountAmount.HasValue &&
                                tempDiscount > coupon.MaxDiscountAmount.Value)
                            {
                                tempDiscount = coupon.MaxDiscountAmount.Value;
                            }

                            if (tempDiscount > bestDiscount)
                                bestDiscount = tempDiscount;
                        }
                    }

                    item.Discount = bestDiscount;

                    total += itemTotal;
                    totalDiscount += bestDiscount;
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
                { "amount", (int)(finalAmount * 100) }, // ✅ after discount
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
         payment_method, payment_status, order_status, razorpay_order_id)
        VALUES (@email, @address, @total, @discount, @final,
                @method, @paymentStatus, @status, @razorpayId)
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

                    orderId = (Guid)await cmd.ExecuteScalarAsync();
                }

                // ============================
                // 🔹 7. INSERT ORDER ITEMS
                // ============================
                foreach (var item in cartItems)
                {
                    string itemQuery = @"
            INSERT INTO order_items 
            (order_id, product_id, quantity, price, discount)
            VALUES (@oid, @pid, @qty, @price, @discount)";

                    using var cmd = new NpgsqlCommand(itemQuery, conn, transaction);
                    cmd.Parameters.AddWithValue("@oid", orderId);
                    cmd.Parameters.AddWithValue("@pid", item.ProductId);
                    cmd.Parameters.AddWithValue("@qty", item.Quantity);
                    cmd.Parameters.AddWithValue("@price", item.Price);
                    cmd.Parameters.AddWithValue("@discount", item.Discount);

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
                    finalAmount
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
    }
}