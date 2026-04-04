using elemechWisetrack.Models;
using Npgsql;

namespace elemechWisetrack.DataBaseLayer
{
    public interface IDataBaseLayer_User
    {
        Task<object> AddWishListProduct(string productId, string email, string ipAddress);
        Task<object> GetWishListProduct(string email, string ipAddress);
        Task<object> DeleteWishListProduct(string productId, string email, string ipAddress);
        Task<object> CreateContact(ContactUsModel model);
        Task<object> GetAllContacts();
        Task<object> GetContactById(Guid id);
        Task<object> UpdateStatus(Guid id, string status);
        Task<object> DeleteContact(Guid id);
    }

    public partial interface IDataBaseLayer : IDataBaseLayer_User { }

    public partial class DataBaseLayer
    {
        // ✅ ADD WISHLIST (Guest + User + Migration)
        public async Task<object> AddWishListProduct(string productId, string email, string ipAddress)
        {
            try
            {
                using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();

                Guid pid = Guid.Parse(productId);

                // 🔄 STEP 1: MIGRATE IP → EMAIL AFTER LOGIN
                if (!string.IsNullOrEmpty(email))
                {
                    var migrateQuery = @"
                        UPDATE wishlist
                        SET email = @Email, ip_address = NULL
                        WHERE ip_address = @IpAddress
                    ";

                    using var migrateCmd = new NpgsqlCommand(migrateQuery, con);
                    migrateCmd.Parameters.AddWithValue("@Email", email);
                    migrateCmd.Parameters.AddWithValue("@IpAddress", ipAddress ?? "");

                    await migrateCmd.ExecuteNonQueryAsync();
                }

                // ✅ STEP 2: INSERT WITHOUT DUPLICATE
                var query = @"
                INSERT INTO wishlist (email, ip_address, product_id)
                SELECT @Email, @IpAddress, @ProductId
                WHERE NOT EXISTS (
                    SELECT 1 FROM wishlist 
                    WHERE product_id = @ProductId
                    AND (
                        (email = @Email AND @Email IS NOT NULL)
                        OR
                        (ip_address = @IpAddress AND @Email IS NULL)
                    )
                );
                ";

                using var cmd = new NpgsqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Email", (object?)email ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@IpAddress", (object?)ipAddress ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ProductId", pid);

                await cmd.ExecuteNonQueryAsync();

                return new { success = true, message = "Added to wishlist" };
            }
            catch (Exception ex)
            {
                return new { success = false, message = ex.Message };
            }
        }

        // ✅ GET WISHLIST
        public async Task<object> GetWishListProduct(string email, string ipAddress)
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

    (
        SELECT STRING_AGG(DISTINCT col.name, ', ')
        FROM product_colors pc
        JOIN colors col ON pc.colorid = col.id
        WHERE pc.productid = p.id
    ) AS colors,

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

WHERE 
(
    (w.email = @Email AND @Email IS NOT NULL)
    OR
    (w.ip_address = @IpAddress AND @Email IS NULL)
)

ORDER BY w.created_at DESC
";

                using var cmd = new NpgsqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Email", (object?)email ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@IpAddress", (object?)ipAddress ?? DBNull.Value);

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

                return new { success = true, data = list };
            }
            catch (Exception ex)
            {
                return new { success = false, message = ex.Message };
            }
        }

        // ✅ DELETE WISHLIST
        public async Task<object> DeleteWishListProduct(string productId, string email, string ipAddress)
        {
            try
            {
                using var con = new NpgsqlConnection(DbConnection);
                await con.OpenAsync();

                var query = @"
DELETE FROM wishlist 
WHERE product_id = @ProductId
AND (
    (email = @Email AND @Email IS NOT NULL)
    OR
    (ip_address = @IpAddress AND @Email IS NULL)
)
";

                using var cmd = new NpgsqlCommand(query, con);
                cmd.Parameters.AddWithValue("@ProductId", Guid.Parse(productId));
                cmd.Parameters.AddWithValue("@Email", (object?)email ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@IpAddress", (object?)ipAddress ?? DBNull.Value);

                await cmd.ExecuteNonQueryAsync();

                return new { success = true, message = "Removed from wishlist" };
            }
            catch (Exception ex)
            {
                return new { success = false, message = ex.Message };
            }
        }

        public async Task<object> CreateContact(ContactUsModel model)
        {
            try
            {
                await using var conn = new NpgsqlConnection(DbConnection);
                await conn.OpenAsync();

                string query = @"
                INSERT INTO contact_us (name, email, phone, subject, message)
                VALUES (@Name, @Email, @Phone, @Subject, @Message)
                RETURNING id;
            ";

                await using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Name", model.Name ?? "");
                cmd.Parameters.AddWithValue("@Email", model.Email ?? "");
                cmd.Parameters.AddWithValue("@Phone", model.Phone ?? "");
                cmd.Parameters.AddWithValue("@Subject", model.Subject ?? "");
                cmd.Parameters.AddWithValue("@Message", model.Message ?? "");

                var id = await cmd.ExecuteScalarAsync();

                return new ApiResponse
                {
                    Success = true,
                    Message = "Contact submitted successfully",
                    Data = new { id }
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        // ================================
        // 2. GET ALL CONTACTS
        // ================================
        public async Task<object> GetAllContacts()
        {
            try
            {
                await using var conn = new NpgsqlConnection(DbConnection);
                await conn.OpenAsync();

                string query = "SELECT * FROM contact_us ORDER BY created_at DESC";

                var list = new List<object>();

                await using var cmd = new NpgsqlCommand(query, conn);
                await using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    list.Add(new
                    {
                        id = reader["id"],
                        name = reader["name"],
                        email = reader["email"],
                        phone = reader["phone"],
                        subject = reader["subject"],
                        message = reader["message"],
                        status = reader["status"],
                        created_at = reader["created_at"]
                    });
                }

                return new ApiResponse
                {
                    Success = true,
                    Data = list
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        // ================================
        // 3. GET SINGLE CONTACT
        // ================================
        public async Task<object> GetContactById(Guid id)
        {
            try
            {
                await using var conn = new NpgsqlConnection(DbConnection);
                await conn.OpenAsync();

                string query = "SELECT * FROM contact_us WHERE id=@Id";

                await using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);

                await using var reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var data = new
                    {
                        id = reader["id"],
                        name = reader["name"],
                        email = reader["email"],
                        phone = reader["phone"],
                        subject = reader["subject"],
                        message = reader["message"],
                        status = reader["status"],
                        created_at = reader["created_at"]
                    };

                    return new ApiResponse
                    {
                        Success = true,
                        Data = data
                    };
                }

                return new ApiResponse
                {
                    Success = false,
                    Message = "Contact not found"
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        // ================================
        // 4. UPDATE STATUS
        // ================================
        public async Task<object> UpdateStatus(Guid id, string status)
        {
            try
            {
                await using var conn = new NpgsqlConnection(DbConnection);
                await conn.OpenAsync();

                string query = @"
                UPDATE contact_us 
                SET status=@Status 
                WHERE id=@Id
            ";

                await using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Status", status);
                cmd.Parameters.AddWithValue("@Id", id);

                var rows = await cmd.ExecuteNonQueryAsync();

                return new ApiResponse
                {
                    Success = rows > 0,
                    Message = rows > 0 ? "Status updated" : "Contact not found"
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        // ================================
        // 5. DELETE CONTACT
        // ================================
        public async Task<object> DeleteContact(Guid id)
        {
            try
            {
                await using var conn = new NpgsqlConnection(DbConnection);
                await conn.OpenAsync();

                string query = "DELETE FROM contact_us WHERE id=@Id";

                await using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);

                var rows = await cmd.ExecuteNonQueryAsync();

                return new ApiResponse
                {
                    Success = rows > 0,
                    Message = rows > 0 ? "Deleted successfully" : "Contact not found"
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }
    }


}
