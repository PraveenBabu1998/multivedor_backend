using Npgsql;

namespace elemechWisetrack.DataBaseLayer
{
    public interface IDataBaseLayer_Recent
    {
        Task<object> AddRecentView(string productId, string email, string ipAddress);
        Task<object> GetRecentViews(string email, string ipAddress);
    }

    public partial interface IDataBaseLayer : IDataBaseLayer_Recent { }

    public partial class DataBaseLayer
    {
        // ✅ ADD / UPDATE RECENT VIEW
        public async Task<object> AddRecentView(string productId, string email, string ipAddress)
        {
            using var con = new NpgsqlConnection(DbConnection);
            await con.OpenAsync();

            using var transaction = await con.BeginTransactionAsync();

            try
            {
                var upsertQuery = @"
INSERT INTO recent_views (product_id, email, ip_address, viewed_at)
VALUES (@ProductId, @Email, @IpAddress, NOW())
ON CONFLICT (product_id, COALESCE(email, ''), COALESCE(ip_address, ''))
DO UPDATE SET viewed_at = NOW();
";

                using (var cmd = new NpgsqlCommand(upsertQuery, con, transaction))
                {
                    cmd.Parameters.AddWithValue("@ProductId", Guid.Parse(productId));
                    cmd.Parameters.AddWithValue("@Email", (object?)email ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@IpAddress", (object?)ipAddress ?? DBNull.Value);

                    await cmd.ExecuteNonQueryAsync();
                }

                // ✅ Keep only 20 records per user (email OR IP)
                var deleteQuery = @"
DELETE FROM recent_views
WHERE id IN (
    SELECT id FROM recent_views
    WHERE 
        (email = @Email OR (email IS NULL AND ip_address = @IpAddress))
    ORDER BY viewed_at DESC
    OFFSET 20
);
";

                using (var cmd = new NpgsqlCommand(deleteQuery, con, transaction))
                {
                    cmd.Parameters.AddWithValue("@Email", (object?)email ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@IpAddress", (object?)ipAddress ?? DBNull.Value);

                    await cmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();

                return new { success = true, message = "Recent updated (Login + Guest supported)" };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new { success = false, message = ex.Message };
            }
        }

        // ✅ GET RECENT PRODUCTS
        public async Task<object> GetRecentViews(string email, string ipAddress)
        {
            using var con = new NpgsqlConnection(DbConnection);
            await con.OpenAsync();

            var query = @"
SELECT 
    rv.product_id, 
    p.name, 
    p.price AS original_price,
    p.discountprice,
    p.mainimage, 
    rv.viewed_at
FROM recent_views rv
JOIN products p ON p.id = rv.product_id
WHERE 
    (rv.email = @Email OR (rv.email IS NULL AND rv.ip_address = @IpAddress))
ORDER BY rv.viewed_at DESC
LIMIT 20;
";

            using var cmd = new NpgsqlCommand(query, con);

            cmd.Parameters.AddWithValue("@Email", (object?)email ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IpAddress", (object?)ipAddress ?? DBNull.Value);

            using var reader = await cmd.ExecuteReaderAsync();

            var list = new List<object>();

            while (await reader.ReadAsync())
            {
                decimal originalPrice = reader["original_price"] == DBNull.Value
                    ? 0
                    : Convert.ToDecimal(reader["original_price"]);

                decimal salePrice = reader["discountprice"] == DBNull.Value
                    ? originalPrice
                    : Convert.ToDecimal(reader["discountprice"]);

                // ✅ Optional discount %
                decimal discountPercent = originalPrice > 0
                    ? ((originalPrice - salePrice) * 100) / originalPrice
                    : 0;

                list.Add(new
                {
                    productId = reader["product_id"],
                    name = reader["name"],

                    // ✅ BOTH PRICES
                    originalPrice = originalPrice,
                    salePrice = salePrice,
                    discountPercent = Math.Round(discountPercent, 2),

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
