using Npgsql;

namespace elemechWisetrack.DataBaseLayer
{
    public interface IDataBaseLayer_User
    {
        Task<object> AddWishListProduct(string productId, string email);
        Task<object> GetWishListProduct(string email);
        Task<object> DeleteWishListProduct(string productId, string email);
    }

    public partial interface IDataBaseLayer : IDataBaseLayer_User { }

    public partial class DataBaseLayer
    {
        public async Task<object> AddWishListProduct(string productId, string email)
        {
            try
            {
                using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();

                var query = @"INSERT INTO wishlist (email, product_id)
SELECT @Email, @product_id
WHERE NOT EXISTS (
    SELECT 1 FROM wishlist 
    WHERE email = @Email AND product_id = @product_id
);
        ";

                using var cmd = new NpgsqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@product_id", Guid.Parse(productId));

                await cmd.ExecuteNonQueryAsync();

                return new { success = true, message = "Added to wishlist" };
            }
            catch (Exception ex)
            {
                return new { success = false, message = ex.Message };
            }
        }

        public async Task<object> GetWishListProduct(string email)
        {
            try
            {
                using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();

                var query = @"
SELECT 
    w.id,
    w.product_id,
    p.name,
    p.price,
    p.discountprice,
    p.mainimage,
    c.name AS category_name,
    b.name AS brand_name,

    -- Colors (join with colors table)
    (
        SELECT STRING_AGG(DISTINCT col.name, ', ')
        FROM product_colors pc
        JOIN colors col ON pc.colorid = col.id
        WHERE pc.productid = p.id
    ) AS colors,

    -- Sizes (join with sizes table)
    (
        SELECT STRING_AGG(DISTINCT sz.name, ', ')
        FROM product_sizes ps
        JOIN sizes sz ON ps.size_id = sz.id
        WHERE ps.product_id = p.id
    ) AS sizes

FROM wishlist w
JOIN products p ON w.product_id = p.id

LEFT JOIN categories c ON p.categoryid = c.id
LEFT JOIN brands b ON p.brandid = b.id

WHERE w.email = @Email

ORDER BY w.created_at DESC
";

                using var cmd = new NpgsqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Email", email);

                var reader = await cmd.ExecuteReaderAsync();

                var list = new List<object>();

                while (await reader.ReadAsync())
                {
                    list.Add(new
                    {
                        Id = reader.GetGuid(0),
                        ProductId = reader.GetGuid(1),
                        Name = reader.GetString(2),
                        Price = reader.GetDecimal(3),
                        Discountprice = reader.GetDecimal(4),
                        Image = reader.IsDBNull(5) ? null : reader.GetString(5),

                        CategoryName = reader.IsDBNull(6) ? null : reader.GetString(6),
                        BrandName = reader.IsDBNull(7) ? null : reader.GetString(7),

                        Colors = reader.IsDBNull(8) ? "" : reader.GetString(8),
                        Sizes = reader.IsDBNull(9) ? "" : reader.GetString(9)
                    });
                }

                return new
                {
                    success = true,
                    data = list
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    success = false,
                    message = ex.Message
                };
            }
        }
        public async Task<object> DeleteWishListProduct(string productId, string email)
        {
            try
            {
                using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();

                var query = @"
            DELETE FROM wishlist 
            WHERE product_id = @product_id AND email = @Email
        ";

                using var cmd = new NpgsqlCommand(query, con);
                cmd.Parameters.AddWithValue("@product_id", Guid.Parse(productId));
                cmd.Parameters.AddWithValue("@Email", email);

                await cmd.ExecuteNonQueryAsync();

                return new { success = true, message = "Removed from wishlist" };
            }
            catch (Exception ex)
            {
                return new { success = false, message = ex.Message };
            }
        }
    }


}
