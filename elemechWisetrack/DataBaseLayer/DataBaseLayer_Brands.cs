using CareerCracker.S3Services;
using elemechWisetrack.Models;
using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace elemechWisetrack.DataBaseLayer
{
    public interface IDataBaseLayer_Brands
    {
        Task<object> AddBrands(string userEmail,[FromBody] BrandInsertModel request, string slug);
        Task<List<BrandModel>> GetBrands();
        Task<BrandModel> GetBrandById(Guid id);
        Task<object> UpdateBrandsById(Guid id, string userEmail, [FromBody] BrandInsertModel request);
        Task<object> DeleteBrandsById(Guid id);
        Task<object> ToggleBrandsById(Guid id);
    }

    public partial interface IDataBaseLayer : IDataBaseLayer_Brands { }

    public partial class DataBaseLayer
    {
        public async Task<object> AddBrands(string userEmail, BrandInsertModel request, string slug)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            // 🔹 Get userId
            string userQuery = @"SELECT ""Id"" FROM ""AspNetUsers"" WHERE ""Email""=@Email LIMIT 1";

            Guid userId;
            using (var cmd = new NpgsqlCommand(userQuery, conn))
            {
                cmd.Parameters.AddWithValue("@Email", userEmail);
                var result = await cmd.ExecuteScalarAsync();

                if (result == null)
                    return new { success = false, message = "User not found" };

                userId = Guid.Parse(result.ToString());
            }

            // 🔹 Save Image
            string? imagePath = null;

            if (request.Logo != null)
            {
                imagePath = await S3StorageHelper.UploadFileAsync(request.Logo, "brands");
            }

            // 🔹 Insert
            string query = @"
INSERT INTO brands
(id,name,slug,description,logo,isactive,createdby,createddate)
VALUES
(@id,@name,@slug,@description,@logo,@isactive,@createdby,@createddate)";

            using var insertCmd = new NpgsqlCommand(query, conn);

            insertCmd.Parameters.AddWithValue("@id", Guid.NewGuid());
            insertCmd.Parameters.AddWithValue("@name", request.Name);
            insertCmd.Parameters.AddWithValue("@slug", slug);
            insertCmd.Parameters.AddWithValue("@description", (object?)request.Description ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@logo", (object?)imagePath ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@isactive", request.IsActive);
            insertCmd.Parameters.AddWithValue("@createdby", userId);
            insertCmd.Parameters.AddWithValue("@createddate", DateTime.UtcNow);

            await insertCmd.ExecuteNonQueryAsync();

            return new { success = true, message = "Brand added", logo = imagePath };
        }

        public async Task<List<BrandModel>> GetBrands()
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            string query = "SELECT * FROM brands ORDER BY createddate DESC";

            using var cmd = new NpgsqlCommand(query, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            var list = new List<BrandModel>();

            while (await reader.ReadAsync())
            {
                list.Add(new BrandModel
                {
                    Id = reader.GetGuid(0),
                    Name = reader["name"]?.ToString(),
                    Slug = reader["slug"]?.ToString(),
                    Description = reader["description"]?.ToString(),
                    Logo = reader["logo"]?.ToString(), // ✅ return image path
                    IsActive = Convert.ToBoolean(reader["isactive"]),
                    CreatedDate = Convert.ToDateTime(reader["createddate"])
                });
            }

            return list;
        }
        public async Task<BrandModel?> GetBrandById(Guid id)
        {
            using (var conn = new NpgsqlConnection(DbConnection))
            {
                await conn.OpenAsync();

                string query = @"SELECT id, name, slug, description, logo, 
                                isactive, createdby, createddate, updateddate
                         FROM brands 
                         WHERE id = @id";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new BrandModel
                            {
                                Id = reader.GetGuid(0),
                                Name = reader.IsDBNull(1) ? null : reader.GetString(1),
                                Slug = reader.IsDBNull(2) ? null : reader.GetString(2),
                                Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                                Logo = reader.IsDBNull(4) ? null : reader.GetString(4),
                                IsActive = reader.GetBoolean(5),
                                CreatedDate = reader.GetDateTime(7)
                            };
                        }
                    }
                }
            }

            return null;
        }

        public async Task<object> UpdateBrandsById(Guid id, string userEmail, BrandInsertModel request)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            // 🔹 Get existing image
            string getQuery = "SELECT logo FROM brands WHERE id=@id";
            string oldImage = null;

            using (var cmd = new NpgsqlCommand(getQuery, conn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                var result = await cmd.ExecuteScalarAsync();
                oldImage = result?.ToString();
            }

            // 🔹 Upload new image (if exists)
            string newImage = oldImage;

            if (request.Logo != null)
            {
                if (!string.IsNullOrEmpty(oldImage))
                    await S3StorageHelper.DeleteStoredMediaAsync(oldImage);

                newImage = await S3StorageHelper.UploadFileAsync(request.Logo, "brands");
            }

            // 🔹 Update query
            string updateQuery = @"
UPDATE brands SET
name=@name,
slug=@slug,
description=@description,
logo=@logo,
isactive=@isactive,
updateddate=@updateddate
WHERE id=@id";

            using var updateCmd = new NpgsqlCommand(updateQuery, conn);

            updateCmd.Parameters.AddWithValue("@id", id);
            updateCmd.Parameters.AddWithValue("@name", request.Name);
            updateCmd.Parameters.AddWithValue("@slug", request.Name.ToLower().Replace(" ", "-"));
            updateCmd.Parameters.AddWithValue("@description", (object?)request.Description ?? DBNull.Value);
            updateCmd.Parameters.AddWithValue("@logo", (object?)newImage ?? DBNull.Value);
            updateCmd.Parameters.AddWithValue("@isactive", request.IsActive);
            updateCmd.Parameters.AddWithValue("@updateddate", DateTime.UtcNow);

            await updateCmd.ExecuteNonQueryAsync();

            return new { success = true, message = "Brand updated", logo = newImage };
        }

        public async Task<object> DeleteBrandsById(Guid id)
        {
            try
            {
                using (var conn = new NpgsqlConnection(DbConnection))
                {
                    await conn.OpenAsync();

                    if (id == Guid.Empty)
                    {
                        return new
                        {
                            Success = false,
                            Message = "Invalid Brand Id"
                        };
                    }

                    string deleteQuery = @"DELETE FROM brands WHERE id = @id;";

                    using (var cmd = new NpgsqlCommand(deleteQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);

                        int rowsAffected = await cmd.ExecuteNonQueryAsync();

                        if (rowsAffected == 0)
                        {
                            return new
                            {
                                Success = false,
                                Message = "Brand not found"
                            };
                        }
                    }

                    return new
                    {
                        Success = true,
                        Message = "Brand deleted successfully"
                    };
                }
            }
            catch (Exception ex)
            {
                return new
                {
                    Success = false,
                    Message = "Error while deleting brand",
                    Error = ex.Message
                };
            }
        }

        public async Task<object> ToggleBrandsById(Guid id)
        {
            try
            {
                using (var conn = new NpgsqlConnection(DbConnection))
                {
                    await conn.OpenAsync();

                    if (id == Guid.Empty)
                    {
                        return new
                        {
                            Success = false,
                            Message = "Invalid Brand Id"
                        };
                    }

                    // 1️⃣ Check if Brand exists + get current status
                    string checkQuery = @"SELECT isactive FROM brands WHERE id = @id;";

                    bool currentStatus;

                    using (var checkCmd = new NpgsqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@id", id);

                        var result = await checkCmd.ExecuteScalarAsync();

                        if (result == null)
                        {
                            return new
                            {
                                Success = false,
                                Message = "Brand not found"
                            };
                        }

                        currentStatus = (bool)result;
                    }

                    // 2️⃣ Toggle Status
                    bool newStatus = !currentStatus;

                    string updateQuery = @"
                UPDATE brands
                SET isactive = @IsActive,
                    updateddate = @UpdatedDate
                WHERE id = @id;";

                    using (var updateCmd = new NpgsqlCommand(updateQuery, conn))
                    {
                        updateCmd.Parameters.AddWithValue("@IsActive", newStatus);
                        updateCmd.Parameters.AddWithValue("@UpdatedDate", DateTime.UtcNow);
                        updateCmd.Parameters.AddWithValue("@id", id);

                        await updateCmd.ExecuteNonQueryAsync();
                    }

                    return new
                    {
                        Success = true,
                        Message = "Brand status updated successfully",
                        IsActive = newStatus
                    };
                }
            }
            catch (Exception ex)
            {
                return new
                {
                    Success = false,
                    Message = "Error while toggling brand status",
                    Error = ex.Message
                };
            }
        }
    }
}
