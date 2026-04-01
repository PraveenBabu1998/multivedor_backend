using Npgsql;

namespace elemechWisetrack.DataBaseLayer
{
    public interface IDataBaseLayer_Recent
    {
        Task<object> AddRecentView(string productId, string email);
        Task<object> GetRecentViews(string email);
    }

    public partial interface IDataBaseLayer : IDataBaseLayer_Recent { }

    public partial class DataBaseLayer
    {
        // ✅ ADD / UPDATE RECENT VIEW
        public async Task<object> AddRecentView(string productId, string email)
        {
            using var con = new NpgsqlConnection(DbConnection);
            await con.OpenAsync();

            using var transaction = await con.BeginTransactionAsync();

            try
            {
                // ✅ 1. INSERT or UPDATE (avoid duplicate)
                var upsertQuery = @"
        INSERT INTO recent_views (product_id, email, viewed_at)
        VALUES (@ProductId, @Email, NOW())
        ON CONFLICT (product_id, email)
        DO UPDATE SET viewed_at = NOW();
        ";

                using (var cmd = new NpgsqlCommand(upsertQuery, con, transaction))
                {
                    cmd.Parameters.AddWithValue("@ProductId", Guid.Parse(productId));
                    cmd.Parameters.AddWithValue("@Email", email);
                    await cmd.ExecuteNonQueryAsync();
                }

                // ✅ 2. DELETE OLD RECORDS (keep only latest 20)
                var deleteQuery = @"
        DELETE FROM recent_views
        WHERE id IN (
            SELECT id FROM recent_views
            WHERE email = @Email
            ORDER BY viewed_at DESC
            OFFSET 20
        );
        ";

                using (var cmd = new NpgsqlCommand(deleteQuery, con, transaction))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    await cmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();

                return new { success = true, message = "Recent updated (max 20 maintained)" };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new { success = false, message = ex.Message };
            }
        }

        // ✅ GET RECENT PRODUCTS
        public async Task<object> GetRecentViews(string email)
        {
            using var con = new NpgsqlConnection(DbConnection);
            await con.OpenAsync();

            var query = @"
            SELECT rv.product_id, p.name, p.price, p.mainimage, rv.viewed_at
            FROM recent_views rv
            JOIN products p ON p.id = rv.product_id
            WHERE rv.email = @Email
            ORDER BY rv.viewed_at DESC
            LIMIT 20;
            ";

            using var cmd = new NpgsqlCommand(query, con);
            cmd.Parameters.AddWithValue("@Email", email);

            using var reader = await cmd.ExecuteReaderAsync();

            var list = new List<object>();

            while (await reader.ReadAsync())
            {
                list.Add(new
                {
                    productId = reader["product_id"],
                    name = reader["name"],
                    price = reader["price"],
                    image = reader["mainimage"],
                    viewedAt = reader["viewed_at"]
                });
            }

            return new
            {
                success = true,
                data = list
            };
        }
    }
}
