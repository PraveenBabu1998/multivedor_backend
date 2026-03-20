using elemechWisetrack.Models;
using Npgsql;

namespace elemechWisetrack.DataBaseLayer
{
    public interface IDataBaseLayer_AddToCart
    {
        Task<object> AddToCart(string userEmail, AddToCartModel model);
        Task<object> GetCart(string userEmail);
        Task<object> UpdateCart(string userEmail, UpdateCartModel model);
        Task<object> RemoveItem(string userEmail, Guid productId);
        Task<object> ClearCart(string userEmail);
    }

    public partial interface IDataBaseLayer : IDataBaseLayer_AddToCart { }

    public partial class DataBaseLayer
    {
        
        // ✅ Get userId from Email
        private async Task<Guid?> GetUserIdByEmail(string userEmail, NpgsqlConnection conn)
        {
            string query = @"SELECT ""Id"" FROM ""AspNetUsers"" 
                             WHERE ""Email"" = @Email LIMIT 1";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Email", userEmail);

            var result = await cmd.ExecuteScalarAsync();

            if (result == null)
                return null;

            return Guid.Parse(result.ToString());
        }

        // ✅ Get or Create Cart
        private async Task<Guid> GetOrCreateCart(Guid userId, NpgsqlConnection conn)
        {
            string query = @"SELECT id FROM carts WHERE user_id = @user_id LIMIT 1";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@user_id", userId);

            var result = await cmd.ExecuteScalarAsync();

            if (result != null)
                return (Guid)result;

            string insertQuery = @"INSERT INTO carts (user_id) 
                                   VALUES (@user_id) 
                                   RETURNING id";

            using var insertCmd = new NpgsqlCommand(insertQuery, conn);
            insertCmd.Parameters.AddWithValue("@user_id", userId);

            return (Guid)await insertCmd.ExecuteScalarAsync();
        }

        // ✅ Add to Cart
        public async Task<object> AddToCart(string userEmail, AddToCartModel model)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            var userId = await GetUserIdByEmail(userEmail, conn);
            if (userId == null)
                return new { Success = false, Message = "User not found" };

            var cartId = await GetOrCreateCart(userId.Value, conn);

            string query = @"
                INSERT INTO cart_items (cart_id, product_id, quantity, price)
                VALUES (@cart_id, @product_id, @quantity, @price)
                ON CONFLICT (cart_id, product_id)
                DO UPDATE SET 
                    quantity = cart_items.quantity + EXCLUDED.quantity,
                    updated_at = CURRENT_TIMESTAMP";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@cart_id", cartId);
            cmd.Parameters.AddWithValue("@product_id", model.ProductId);
            cmd.Parameters.AddWithValue("@quantity", model.Quantity);
            cmd.Parameters.AddWithValue("@price", model.Price);

            await cmd.ExecuteNonQueryAsync();

            return new { Success = true, Message = "Added to cart" };
        }

        // ✅ Get Cart
        public async Task<object> GetCart(string userEmail)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            var userId = await GetUserIdByEmail(userEmail, conn);

            if (userId == null)
                return new { Success = false, Message = "User not found" };

            var cartId = await GetOrCreateCart(userId.Value, conn);

            // 🔥 JOIN with products table
            string query = @"
        SELECT 
            ci.product_id,
            ci.quantity,
            ci.price,
            p.name,
            p.mainimage,
            p.price AS product_price
        FROM cart_items ci
        JOIN products p ON p.id = ci.product_id
        WHERE ci.cart_id = @cart_id";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@cart_id", cartId);

            using var reader = await cmd.ExecuteReaderAsync();

            var list = new List<object>();

            while (await reader.ReadAsync())
            {
                list.Add(new
                {
                    ProductId = reader.GetGuid(0),
                    Quantity = reader.GetInt32(1),
                    Price = reader.GetDecimal(2), // cart price
                    ProductName = reader.GetString(3),
                    ProductImage = reader.IsDBNull(4) ? null : reader.GetString(4),
                    CurrentPrice = reader.GetDecimal(5), // latest product price
                    Total = reader.GetInt32(1) * reader.GetDecimal(2)
                });
            }

            return list;
        }

        // ✅ Update Cart
        public async Task<object> UpdateCart(string userEmail, UpdateCartModel model)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            var userId = await GetUserIdByEmail(userEmail, conn);
            if (userId == null)
                return new { Success = false, Message = "User not found" };

            var cartId = await GetOrCreateCart(userId.Value, conn);

            string query = @"UPDATE cart_items 
                             SET quantity = @quantity, updated_at = CURRENT_TIMESTAMP
                             WHERE cart_id = @cart_id AND product_id = @product_id";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@cart_id", cartId);
            cmd.Parameters.AddWithValue("@product_id", model.ProductId);
            cmd.Parameters.AddWithValue("@quantity", model.Quantity);

            await cmd.ExecuteNonQueryAsync();

            return new { Success = true, Message = "Cart updated" };
        }

        // ✅ Remove Item
        public async Task<object> RemoveItem(string userEmail, Guid productId)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            var userId = await GetUserIdByEmail(userEmail, conn);
            if (userId == null)
                return new { Success = false, Message = "User not found" };

            var cartId = await GetOrCreateCart(userId.Value, conn);

            string query = @"DELETE FROM cart_items 
                             WHERE cart_id = @cart_id AND product_id = @product_id";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@cart_id", cartId);
            cmd.Parameters.AddWithValue("@product_id", productId);

            await cmd.ExecuteNonQueryAsync();

            return new { Success = true, Message = "Item removed" };
        }

        // ✅ Clear Cart
        public async Task<object> ClearCart(string userEmail)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            var userId = await GetUserIdByEmail(userEmail, conn);
            if (userId == null)
                return new { Success = false, Message = "User not found" };

            var cartId = await GetOrCreateCart(userId.Value, conn);

            string query = @"DELETE FROM cart_items WHERE cart_id = @cart_id";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@cart_id", cartId);

            await cmd.ExecuteNonQueryAsync();

            return new { Success = true, Message = "Cart cleared" };
        }
    }
}