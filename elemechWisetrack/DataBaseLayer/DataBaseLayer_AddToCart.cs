using elemechWisetrack.Models;
using Npgsql;

namespace elemechWisetrack.DataBaseLayer
{
    public interface IDataBaseLayer_AddToCart
    {
        Task<object> AddToCart(string email, string ip, AddToCartModel model);
        Task<object> GetCart(string email, string ip, string couponCode);
        Task<object> UpdateCart(string email, string ip, UpdateCartModel model);
        Task<object> RemoveItem(string email, string ip, Guid productId);
        Task<object> ClearCart(string email, string ip);
        Task<object> UpdateCartQuantity(string email, UpdateCartQuantityModel model);
    }

    public partial interface IDataBaseLayer : IDataBaseLayer_AddToCart { }

    public partial class DataBaseLayer
    {
       

        private async Task<Guid?> GetUserId(string email, NpgsqlConnection conn)
        {
            if (string.IsNullOrEmpty(email)) return null;

            string query = @"SELECT ""Id"" FROM ""AspNetUsers"" WHERE ""Email""=@Email LIMIT 1";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Email", email);

            var result = await cmd.ExecuteScalarAsync();
            return result == null ? null : Guid.Parse(result.ToString());
        }

        private Guid ConvertIpToGuid(string ip)
        {
            if (string.IsNullOrEmpty(ip))
                ip = "0.0.0.0";

            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(ip));
                return new Guid(hash);
            }
        }

        private async Task<Guid> GetOrCreateCart(Guid? userId, string ip, NpgsqlConnection conn)
        {
            Guid guestGuid = ConvertIpToGuid(ip);

            string query = userId != null
                ? @"SELECT id FROM carts WHERE user_id = @user_id LIMIT 1"
                : @"SELECT id FROM carts WHERE guest_id = @guest_id LIMIT 1";

            using var cmd = new NpgsqlCommand(query, conn);

            if (userId != null)
                cmd.Parameters.AddWithValue("@user_id", userId);
            else
                cmd.Parameters.AddWithValue("@guest_id", guestGuid); // ✅ FIX

            var result = await cmd.ExecuteScalarAsync();

            if (result != null)
                return (Guid)result;

            string insert = userId != null
                ? @"INSERT INTO carts (user_id) VALUES (@user_id) RETURNING id"
                : @"INSERT INTO carts (guest_id) VALUES (@guest_id) RETURNING id";

            using var insertCmd = new NpgsqlCommand(insert, conn);

            if (userId != null)
                insertCmd.Parameters.AddWithValue("@user_id", userId);
            else
                insertCmd.Parameters.AddWithValue("@guest_id", guestGuid); // ✅ FIX

            return (Guid)await insertCmd.ExecuteScalarAsync();
        }

        public async Task<object> AddToCart(string email, string ip, AddToCartModel model)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            var userId = await GetUserId(email, conn);
            var cartId = await GetOrCreateCart(userId, ip, conn);

            string query = @"
                INSERT INTO cart_items (cart_id, product_id, quantity, price)
                VALUES (@cart_id, @product_id, @quantity, @price)
                ON CONFLICT (cart_id, product_id)
                DO UPDATE SET quantity = cart_items.quantity + EXCLUDED.quantity";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@cart_id", cartId);
            cmd.Parameters.AddWithValue("@product_id", model.ProductId);
            cmd.Parameters.AddWithValue("@quantity", model.Quantity);
            cmd.Parameters.AddWithValue("@price", model.Price);

            await cmd.ExecuteNonQueryAsync();

            return new { Success = true };
        }
        //    public async Task<object> GetCart(string email, string ip)
        //    {
        //        using var conn = new NpgsqlConnection(DbConnection);
        //        await conn.OpenAsync();

        //        var userId = await GetUserId(email, conn);

        //        Guid? userCartId = null;
        //        Guid? guestCartId = null;

        //        var guestGuid = ConvertIpToGuid(ip);

        //        // ============================
        //        // STEP 1: GET GUEST CART
        //        // ============================
        //        string guestQuery = @"SELECT id FROM carts WHERE guest_id = @guest_id LIMIT 1";
        //        using (var guestCmd = new NpgsqlCommand(guestQuery, conn))
        //        {
        //            guestCmd.Parameters.AddWithValue("@guest_id", guestGuid);
        //            var result = await guestCmd.ExecuteScalarAsync();
        //            if (result != null)
        //                guestCartId = (Guid)result;
        //        }

        //        // ============================
        //        // STEP 2: USER CART + MERGE
        //        // ============================
        //        if (userId != null)
        //        {
        //            userCartId = await GetOrCreateCart(userId, ip, conn);

        //            if (guestCartId != null)
        //            {
        //                string mergeQuery = @"
        //        INSERT INTO cart_items (cart_id, product_id, quantity, price)
        //        SELECT @userCartId, product_id, quantity, price
        //        FROM cart_items
        //        WHERE cart_id = @guestCartId
        //        ON CONFLICT (cart_id, product_id)
        //        DO UPDATE SET 
        //            quantity = cart_items.quantity + EXCLUDED.quantity,
        //            updated_at = CURRENT_TIMESTAMP";

        //                using var mergeCmd = new NpgsqlCommand(mergeQuery, conn);
        //                mergeCmd.Parameters.AddWithValue("@userCartId", userCartId);
        //                mergeCmd.Parameters.AddWithValue("@guestCartId", guestCartId);
        //                await mergeCmd.ExecuteNonQueryAsync();

        //                string deleteQuery = @"DELETE FROM carts WHERE id = @guestCartId";
        //                using var deleteCmd = new NpgsqlCommand(deleteQuery, conn);
        //                deleteCmd.Parameters.AddWithValue("@guestCartId", guestCartId);
        //                await deleteCmd.ExecuteNonQueryAsync();
        //            }
        //        }

        //        // ============================
        //        // STEP 3: FINAL CART ID
        //        // ============================
        //        Guid finalCartId;

        //        if (userId != null)
        //            finalCartId = userCartId.Value;
        //        else if (guestCartId != null)
        //            finalCartId = guestCartId.Value;
        //        else
        //            finalCartId = await GetOrCreateCart(null, ip, conn);

        //        // ============================
        //        // STEP 4: FETCH CART ITEMS
        //        // ============================
        //        string query = @"
        //SELECT ci.product_id, ci.quantity, ci.price,
        //       p.name, p.mainimage, p.price
        //FROM cart_items ci
        //JOIN products p ON p.id = ci.product_id
        //WHERE ci.cart_id = @cart_id";

        //        using var cmd = new NpgsqlCommand(query, conn);
        //        cmd.Parameters.AddWithValue("@cart_id", finalCartId);

        //        using var reader = await cmd.ExecuteReaderAsync();

        //        var list = new List<CartResponseModel>();
        //        decimal subTotal = 0;

        //        while (await reader.ReadAsync())
        //        {
        //            var quantity = reader.GetInt32(1);
        //            var price = reader.GetDecimal(2);

        //            var total = quantity * price;
        //            subTotal += total;

        //            list.Add(new CartResponseModel
        //            {
        //                ProductId = reader.GetGuid(0),
        //                Quantity = quantity,
        //                Price = price,
        //                ProductName = reader.GetString(3),
        //                ProductImage = reader.IsDBNull(4) ? null : reader.GetString(4),
        //                CurrentPrice = reader.GetDecimal(5),
        //                Total = total,
        //                Discount = 0
        //            });
        //        }

        //        await reader.CloseAsync();

        //        // ============================
        //        // STEP 5: FETCH ALL COUPONS
        //        // ============================
        //        decimal totalDiscount = 0;

        //        if (!string.IsNullOrEmpty(email) && list.Count > 0)
        //        {
        //            var productIds = list.Select(x => x.ProductId).ToArray();

        //            string couponQuery = @"
        //    SELECT cu.product_id,
        //           c.discount_type,
        //           c.discount_value,
        //           c.min_order_amount,
        //           c.max_discount_amount
        //    FROM coupon_usage cu
        //    JOIN coupons c ON c.id = cu.coupon_id
        //    WHERE cu.user_email = @user_email
        //    AND cu.product_id = ANY(@product_ids)
        //    AND c.is_active = TRUE
        //    AND c.start_date <= NOW()
        //    AND c.end_date >= NOW()";

        //            using var couponCmd = new NpgsqlCommand(couponQuery, conn);
        //            couponCmd.Parameters.AddWithValue("@user_email", email);
        //            couponCmd.Parameters.AddWithValue("@product_ids", productIds);

        //            using var couponReader = await couponCmd.ExecuteReaderAsync();

        //            var couponMap = new Dictionary<Guid, List<CouponModel>>();

        //            while (await couponReader.ReadAsync())
        //            {
        //                var productId = couponReader.GetGuid(0);

        //                var coupon = new CouponModel
        //                {
        //                    DiscountType = couponReader["discount_type"]?.ToString(),
        //                    DiscountValue = Convert.ToDecimal(couponReader["discount_value"]),
        //                    MinOrderAmount = Convert.ToDecimal(couponReader["min_order_amount"]),
        //                    MaxDiscountAmount = couponReader["max_discount_amount"] == DBNull.Value
        //                        ? (decimal?)null
        //                        : Convert.ToDecimal(couponReader["max_discount_amount"])
        //                };

        //                if (!couponMap.ContainsKey(productId))
        //                    couponMap[productId] = new List<CouponModel>();

        //                couponMap[productId].Add(coupon);
        //            }

        //            await couponReader.CloseAsync();

        //            // ============================
        //            // APPLY BEST COUPON PER PRODUCT
        //            // ============================
        //            foreach (var item in list)
        //            {
        //                if (!couponMap.ContainsKey(item.ProductId))
        //                    continue;

        //                decimal bestDiscount = 0;

        //                foreach (var coupon in couponMap[item.ProductId])
        //                {
        //                    if (item.Total < coupon.MinOrderAmount)
        //                        continue;

        //                    decimal tempDiscount = 0;

        //                    if (coupon.DiscountType == "percentage")
        //                    {
        //                        tempDiscount = (item.Total * coupon.DiscountValue) / 100;
        //                    }
        //                    else if (coupon.DiscountType == "fixed")
        //                    {
        //                        tempDiscount = coupon.DiscountValue;
        //                    }

        //                    if (coupon.MaxDiscountAmount.HasValue &&
        //                        tempDiscount > coupon.MaxDiscountAmount.Value)
        //                    {
        //                        tempDiscount = coupon.MaxDiscountAmount.Value;
        //                    }

        //                    if (tempDiscount > bestDiscount)
        //                        bestDiscount = tempDiscount;
        //                }

        //                item.Discount = bestDiscount;
        //                totalDiscount += bestDiscount;
        //            }
        //        }

        //        // ============================
        //        // FINAL TOTAL
        //        // ============================
        //        decimal finalTotal = subTotal - totalDiscount;

        //        return new
        //        {
        //            Items = list,
        //            SubTotal = subTotal,
        //            Discount = totalDiscount,
        //            FinalTotal = finalTotal
        //        };
        //    }

        public async Task<object> GetCart(string email, string ip, string couponCode)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            var userId = await GetUserId(email, conn);

            Guid? userCartId = null;
            Guid? guestCartId = null;

            var guestGuid = ConvertIpToGuid(ip);

            // ============================
            // STEP 1: GET GUEST CART
            // ============================
            string guestQuery = @"SELECT id FROM carts WHERE guest_id = @guest_id LIMIT 1";
            using (var guestCmd = new NpgsqlCommand(guestQuery, conn))
            {
                guestCmd.Parameters.AddWithValue("@guest_id", guestGuid);
                var result = await guestCmd.ExecuteScalarAsync();
                if (result != null)
                    guestCartId = (Guid)result;
            }

            // ============================
            // STEP 2: USER CART + MERGE
            // ============================
            if (userId != null)
            {
                userCartId = await GetOrCreateCart(userId, ip, conn);

                if (guestCartId != null)
                {
                    string mergeQuery = @"
            INSERT INTO cart_items (cart_id, product_id, quantity, price)
            SELECT @userCartId, product_id, quantity, price
            FROM cart_items
            WHERE cart_id = @guestCartId
            ON CONFLICT (cart_id, product_id)
            DO UPDATE SET 
                quantity = cart_items.quantity + EXCLUDED.quantity,
                updated_at = CURRENT_TIMESTAMP";

                    using var mergeCmd = new NpgsqlCommand(mergeQuery, conn);
                    mergeCmd.Parameters.AddWithValue("@userCartId", userCartId);
                    mergeCmd.Parameters.AddWithValue("@guestCartId", guestCartId);
                    await mergeCmd.ExecuteNonQueryAsync();

                    string deleteQuery = @"DELETE FROM carts WHERE id = @guestCartId";
                    using var deleteCmd = new NpgsqlCommand(deleteQuery, conn);
                    deleteCmd.Parameters.AddWithValue("@guestCartId", guestCartId);
                    await deleteCmd.ExecuteNonQueryAsync();
                }
            }

            // ============================
            // STEP 3: FINAL CART ID
            // ============================
            Guid finalCartId;

            if (userId != null)
                finalCartId = userCartId.Value;
            else if (guestCartId != null)
                finalCartId = guestCartId.Value;
            else
                finalCartId = await GetOrCreateCart(null, ip, conn);

            // ============================
            // STEP 4: FETCH CART ITEMS
            // ============================
            string query = @"
    SELECT ci.product_id, ci.quantity, ci.price,
           p.name, p.mainimage, p.price
    FROM cart_items ci
    JOIN products p ON p.id = ci.product_id
    WHERE ci.cart_id = @cart_id";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@cart_id", finalCartId);

            using var reader = await cmd.ExecuteReaderAsync();

            var list = new List<CartResponseModel>();
            decimal subTotal = 0;

            while (await reader.ReadAsync())
            {
                var quantity = reader.GetInt32(1);
                var price = reader.GetDecimal(2);

                var total = quantity * price;
                subTotal += total;

                list.Add(new CartResponseModel
                {
                    ProductId = reader.GetGuid(0),
                    Quantity = quantity,
                    Price = price,
                    ProductName = reader.GetString(3),
                    ProductImage = reader.IsDBNull(4) ? null : reader.GetString(4),
                    CurrentPrice = reader.GetDecimal(5),
                    Total = total,
                    Discount = 0
                });
            }

            await reader.CloseAsync();

            // ============================
            // STEP 5: APPLY COUPON FROM PARAM
            // ============================
            decimal discount = 0;
            string appliedCoupon = null;

            if (!string.IsNullOrEmpty(couponCode))
            {
                string couponQuery = @"
        SELECT *
        FROM coupons
        WHERE code = @code
        AND is_active = TRUE
        AND start_date <= NOW()
        AND end_date >= NOW()";

                using var couponCmd = new NpgsqlCommand(couponQuery, conn);
                couponCmd.Parameters.AddWithValue("@code", couponCode);

                using var couponReader = await couponCmd.ExecuteReaderAsync();

                if (await couponReader.ReadAsync())
                {
                    var discountType = couponReader["discount_type"]?.ToString();
                    var discountValue = Convert.ToDecimal(couponReader["discount_value"]);
                    var minOrder = Convert.ToDecimal(couponReader["min_order_amount"]);

                    decimal? maxDiscount = couponReader["max_discount_amount"] == DBNull.Value
                        ? (decimal?)null
                        : Convert.ToDecimal(couponReader["max_discount_amount"]);

                    if (subTotal >= minOrder)
                    {
                        if (discountType == "percentage")
                            discount = (subTotal * discountValue) / 100;
                        else if (discountType == "fixed")
                            discount = discountValue;

                        if (maxDiscount.HasValue && discount > maxDiscount.Value)
                            discount = maxDiscount.Value;

                        appliedCoupon = couponCode;
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

                await couponReader.CloseAsync();
            }

            // ============================
            // FINAL TOTAL
            // ============================
            decimal finalTotal = subTotal - discount;

            return new
            {
                success = true,
                Items = list,
                SubTotal = subTotal,
                Discount = discount,
                CouponCode = appliedCoupon,
                FinalTotal = finalTotal
            };
        }

        public async Task<object> UpdateCart(string email, string ip, UpdateCartModel model)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            var userId = await GetUserId(email, conn);
            var cartId = await GetOrCreateCart(userId, ip, conn);

            string query = @"UPDATE cart_items SET quantity=@q WHERE cart_id=@c AND product_id=@p";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@q", model.Quantity);
            cmd.Parameters.AddWithValue("@c", cartId);
            cmd.Parameters.AddWithValue("@p", model.ProductId);

            await cmd.ExecuteNonQueryAsync();
            return new { Success = true };
        }

        public async Task<object> RemoveItem(string email, string ip, Guid productId)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            var userId = await GetUserId(email, conn);
            var cartId = await GetOrCreateCart(userId, ip, conn);

            string query = @"DELETE FROM cart_items WHERE cart_id=@c AND product_id=@p";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@c", cartId);
            cmd.Parameters.AddWithValue("@p", productId);

            await cmd.ExecuteNonQueryAsync();
            return new { Success = true };
        }

        public async Task<object> ClearCart(string email, string ip)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            var userId = await GetUserId(email, conn);
            var cartId = await GetOrCreateCart(userId, ip, conn);

            string query = @"DELETE FROM cart_items WHERE cart_id=@c";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@c", cartId);

            await cmd.ExecuteNonQueryAsync();
            return new { Success = true };
        }

        public async Task<object> UpdateCartQuantity(string email, UpdateCartQuantityModel model)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            using var transaction = await conn.BeginTransactionAsync();

            try
            {
                // 🔹 1. Get UserId
                string getUser = @"SELECT ""Id"" FROM ""AspNetUsers"" WHERE ""Email""=@email LIMIT 1";

                Guid userId;

                using (var cmd = new NpgsqlCommand(getUser, conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@email", email);
                    var result = await cmd.ExecuteScalarAsync();

                    if (result == null)
                        return new { success = false, message = "User not found" };

                    userId = Guid.Parse(result.ToString());
                }

                // 🔹 2. Get Cart Id
                string getCart = @"SELECT id FROM carts WHERE user_id=@uid LIMIT 1";

                Guid cartId;

                using (var cmd = new NpgsqlCommand(getCart, conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@uid", userId);
                    var result = await cmd.ExecuteScalarAsync();

                    if (result == null)
                    {
                        // create cart if not exists
                        string createCart = @"INSERT INTO carts(user_id) VALUES(@uid) RETURNING id";

                        using var createCmd = new NpgsqlCommand(createCart, conn, transaction);
                        createCmd.Parameters.AddWithValue("@uid", userId);

                        cartId = (Guid)await createCmd.ExecuteScalarAsync();
                    }
                    else
                    {
                        cartId = (Guid)result;
                    }
                }

                // 🔹 3. Check if item exists
                string checkItem = @"
        SELECT id, quantity FROM cart_items
        WHERE cart_id=@cartId AND product_id=@pid";

                Guid itemId = Guid.Empty;
                int qty = 0;

                using (var cmd = new NpgsqlCommand(checkItem, conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@cartId", cartId);
                    cmd.Parameters.AddWithValue("@pid", model.ProductId);

                    using var reader = await cmd.ExecuteReaderAsync();

                    if (await reader.ReadAsync())
                    {
                        itemId = reader.GetGuid(0);
                        qty = reader.GetInt32(1);
                    }
                }

                // 🔥 4. HANDLE ACTION

                if (model.Action == "ADD")
                {
                    if (itemId == Guid.Empty)
                    {
                        // 👉 insert new item
                        string getPrice = "SELECT price FROM products WHERE id=@pid";

                        decimal price;

                        using (var cmd = new NpgsqlCommand(getPrice, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@pid", model.ProductId);
                            price = (decimal)await cmd.ExecuteScalarAsync();
                        }

                        string insert = @"
                INSERT INTO cart_items(cart_id, product_id, quantity, price)
                VALUES(@cid, @pid, 1, @price)";

                        using var cmdInsert = new NpgsqlCommand(insert, conn, transaction);
                        cmdInsert.Parameters.AddWithValue("@cid", cartId);
                        cmdInsert.Parameters.AddWithValue("@pid", model.ProductId);
                        cmdInsert.Parameters.AddWithValue("@price", price);

                        await cmdInsert.ExecuteNonQueryAsync();
                    }
                    else
                    {
                        // 👉 increment
                        string update = @"
                UPDATE cart_items
                SET quantity = quantity + 1
                WHERE id=@id";

                        using var cmdUpdate = new NpgsqlCommand(update, conn, transaction);
                        cmdUpdate.Parameters.AddWithValue("@id", itemId);

                        await cmdUpdate.ExecuteNonQueryAsync();
                    }
                }
                else if (model.Action == "REMOVE")
                {
                    if (itemId == Guid.Empty)
                        return new { success = false, message = "Item not in cart" };

                    if (qty <= 1)
                    {
                        // 👉 delete item
                        string delete = "DELETE FROM cart_items WHERE id=@id";

                        using var cmdDelete = new NpgsqlCommand(delete, conn, transaction);
                        cmdDelete.Parameters.AddWithValue("@id", itemId);

                        await cmdDelete.ExecuteNonQueryAsync();
                    }
                    else
                    {
                        // 👉 decrement
                        string update = @"
                UPDATE cart_items
                SET quantity = quantity - 1
                WHERE id=@id";

                        using var cmdUpdate = new NpgsqlCommand(update, conn, transaction);
                        cmdUpdate.Parameters.AddWithValue("@id", itemId);

                        await cmdUpdate.ExecuteNonQueryAsync();
                    }
                }

                await transaction.CommitAsync();

                return new
                {
                    success = true,
                    message = "Cart updated successfully"
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
    }
}