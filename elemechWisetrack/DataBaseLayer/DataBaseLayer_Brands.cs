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
        public async Task<object> AddBrands(string userEmail, [FromBody] BrandInsertModel request, string slug)
        {
            try
            {
                using (var conn = new NpgsqlConnection(DbConnection))
                {
                    await conn.OpenAsync();

                    // =============================
                    // 1️⃣ Get User Id From Email
                    // =============================
                    string getUserQuery = @"
                SELECT ""Id""
                FROM ""AspNetUsers""
                WHERE ""Email"" = @Email
                LIMIT 1;";

                    Guid userId;

                    using (var userCmd = new NpgsqlCommand(getUserQuery, conn))
                    {
                        userCmd.Parameters.AddWithValue("@Email", userEmail);

                        var result = await userCmd.ExecuteScalarAsync();

                        if (result == null)
                        {
                            return new
                            {
                                Success = false,
                                Message = "User not found"
                            };
                        }

                        userId = Guid.Parse(result.ToString());
                    }

                    // =============================
                    // 2️⃣ Insert Brand
                    // =============================
                    string insertQuery = @"
    INSERT INTO brands
    (id, name, slug, description, logo, isactive, createdby, createddate)
    VALUES
    (@Id, @Name, @Slug, @Description, @Logo, @IsActive, @CreatedBy, @CreatedDate);";


                    Guid brandId = Guid.NewGuid();

                    using (var cmd = new NpgsqlCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", brandId);
                        cmd.Parameters.AddWithValue("@Name", request.Name);
                        cmd.Parameters.AddWithValue("@Slug", slug);
                        cmd.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Logo", (object?)request.Logo ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@IsActive", request.IsActive);
                        cmd.Parameters.AddWithValue("@CreatedBy", userId);
                        cmd.Parameters.AddWithValue("@CreatedDate", DateTime.UtcNow);

                        await cmd.ExecuteNonQueryAsync();
                    }

                    return new
                    {
                        Success = true,
                        Message = "Brand added successfully",
                        BrandId = brandId
                    };
                }
            }
            catch (Exception ex)
            {
                return new
                {
                    Success = false,
                    Message = "Error while adding brand",
                    Error = ex.Message
                };
            }
        }

        public async Task<List<BrandModel>> GetBrands()
        {
            try
            {
                using (var conn = new NpgsqlConnection(DbConnection))
                {
                    await conn.OpenAsync();

                    string getQuery = @"SELECT b.id, b.name, b.slug, b.description, 
                                       b.logo, b.isactive, b.createdby, 
                                       b.createddate, b.updateddate 
                                FROM brands b 
                                ORDER BY b.createddate DESC";

                    using (var cmd = new NpgsqlCommand(getQuery, conn))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        var list = new List<BrandModel>();

                        while (await reader.ReadAsync())
                        {
                            list.Add(new BrandModel()
                            {
                                Id = reader.GetGuid(0),
                                Name = reader.IsDBNull(1) ? null : reader.GetString(1),
                                Slug = reader.IsDBNull(2) ? null : reader.GetString(2),
                                Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                                Logo = reader.IsDBNull(4) ? null : reader.GetString(4),
                                IsActive = reader.GetBoolean(5),
                                CreatedDate = reader.GetDateTime(7) // fixed index
                            });
                        }

                        return list;
                    }
                }
            }
            catch (Exception ex)
            {
                // log exception here
                throw; // let controller handle error
            }
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

        public async Task<object> UpdateBrandsById(Guid id, string userEmail, [FromBody] BrandInsertModel request)
        {
            try
            {
                using (var conn = new NpgsqlConnection(DbConnection))
                {
                    await conn.OpenAsync();

                    // =============================
                    // 1️⃣ Validate Brand Id
                    // =============================
                    if (id == Guid.Empty)
                    {
                        return new
                        {
                            Success = false,
                            Message = "Invalid Brand Id"
                        };
                    }

                    // =============================
                    // 2️⃣ Get User Id From Email
                    // =============================
                    string getUserQuery = @"
                SELECT ""Id""
                FROM ""AspNetUsers""
                WHERE ""Email"" = @Email
                LIMIT 1;";

                    Guid userId;

                    using (var userCmd = new NpgsqlCommand(getUserQuery, conn))
                    {
                        userCmd.Parameters.AddWithValue("@Email", userEmail);

                        var result = await userCmd.ExecuteScalarAsync();

                        if (result == null)
                        {
                            return new
                            {
                                Success = false,
                                Message = "User not found"
                            };
                        }

                        userId = Guid.Parse(result.ToString());
                    }

                    // =============================
                    // 3️⃣ Check Brand Exists
                    // =============================
                    string checkQuery = @"SELECT COUNT(1) FROM brands WHERE id = @Id";

                    using (var checkCmd = new NpgsqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@Id", id);

                        var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

                        if (exists == 0)
                        {
                            return new
                            {
                                Success = false,
                                Message = "Brand not found"
                            };
                        }
                    }

                    // =============================
                    // 4️⃣ Generate Slug (Optional)
                    // =============================
                    string slug = request.Name?.Trim().ToLower().Replace(" ", "-");

                    // =============================
                    // 5️⃣ Update Brand
                    // =============================
                    string updateQuery = @"
                UPDATE brands
                SET name = @Name,
                    slug = @Slug,
                    description = @Description,
                    logo = @Logo,
                    isactive = @IsActive,
                    updateddate = @UpdatedDate,
                    createdby = @UpdatedBy
                WHERE id = @Id;";

                    using (var cmd = new NpgsqlCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.Parameters.AddWithValue("@Name", request.Name);
                        cmd.Parameters.AddWithValue("@Slug", slug);
                        cmd.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Logo", (object?)request.Logo ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@IsActive", request.IsActive);
                        cmd.Parameters.AddWithValue("@UpdatedDate", DateTime.UtcNow);
                        cmd.Parameters.AddWithValue("@UpdatedBy", userId);

                        await cmd.ExecuteNonQueryAsync();
                    }

                    return new
                    {
                        Success = true,
                        Message = "Brand updated successfully",
                        BrandId = id
                    };
                }
            }
            catch (Exception ex)
            {
                return new
                {
                    Success = false,
                    Message = "Error while updating brand",
                    Error = ex.Message
                };
            }
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
