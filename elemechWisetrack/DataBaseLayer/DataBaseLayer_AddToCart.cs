using elemechWisetrack.Models;
using Npgsql;

namespace elemechWisetrack.DataBaseLayer
{
    public interface IDataBaseLayer_AddToCart
    {
        Task<object> AddToCart(string email, string ip, AddToCartModel model);
        Task<object> GetCart(string email, string ip);
        Task<object> UpdateCart(string email, string ip, UpdateCartModel model);
        Task<object> RemoveItem(string email, string ip, Guid productId);
        Task<object> ClearCart(string email, string ip);
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

        public async Task<object> GetCart(string email, string ip)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            var userId = await GetUserId(email, conn);

            Guid? userCartId = null;
            Guid? guestCartId = null;

            // ✅ Convert IP → UUID
            var guestGuid = ConvertIpToGuid(ip);

            // ✅ Step 1: Get guest cart
            string guestQuery = @"SELECT id FROM carts WHERE guest_id = @guest_id LIMIT 1";
            using (var guestCmd = new NpgsqlCommand(guestQuery, conn))
            {
                guestCmd.Parameters.AddWithValue("@guest_id", guestGuid);
                var result = await guestCmd.ExecuteScalarAsync();
                if (result != null)
                    guestCartId = (Guid)result;
            }

            // ✅ Step 2: If user logged in → get user cart
            if (userId != null)
            {
                userCartId = await GetOrCreateCart(userId, ip, conn);

                // 🔥 STEP 3: MERGE (ONLY IF BOTH EXIST)
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

                    // ✅ Delete guest cart after merge
                    string deleteQuery = @"DELETE FROM carts WHERE id = @guestCartId";

                    using var deleteCmd = new NpgsqlCommand(deleteQuery, conn);
                    deleteCmd.Parameters.AddWithValue("@guestCartId", guestCartId);

                    await deleteCmd.ExecuteNonQueryAsync();
                }
            }

            // ✅ Step 4: Decide final cartId
            Guid finalCartId;

            if (userId != null)
                finalCartId = userCartId.Value;
            else if (guestCartId != null)
                finalCartId = guestCartId.Value;
            else
                finalCartId = await GetOrCreateCart(null, ip, conn);

            // ✅ Step 5: Fetch cart data
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

            while (await reader.ReadAsync())
            {
                list.Add(new CartResponseModel
                {
                    ProductId = reader.GetGuid(0),
                    Quantity = reader.GetInt32(1),
                    Price = reader.GetDecimal(2),
                    ProductName = reader.GetString(3),
                    ProductImage = reader.IsDBNull(4) ? null : reader.GetString(4),
                    CurrentPrice = reader.GetDecimal(5),
                    Total = reader.GetInt32(1) * reader.GetDecimal(2)
                });
            }

            return list;
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
    }
}