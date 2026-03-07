using elemechWisetrack.Models;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace elemechWisetrack.DataBaseLayer
{
    public interface IDataBaseLayer_colors
    {
        Task<object> AddColors(string userEmail,[FromBody] ProductsCollors request, string baseSlug);
        Task<object> GetColors();
        Task<object> UpdateColor(string userEmail, Guid id, ProductsCollors request, string baseSlug);
        Task<object> ToggleColorStatus(Guid id);
        Task<object> SoftDeleteColor(Guid id);
        Task<object> RestoreColor(Guid id);
        Task<object> DeleteColor(Guid id);
    }

    public partial interface IDataBaseLayer : IDataBaseLayer_colors
    {

    }

    public partial class DataBaseLayer
    {
        public async Task<object> AddColors(string userEmail, ProductsCollors request, string baseSlug)
        
        {
            using (var con = new NpgsqlConnection(DbConnection))
            {
                await con.OpenAsync();

                string addQuery = @"INSERT INTO colors 
        (id, name, hexcode, isactive, isdeleted, createddate, description, slug) 
        VALUES 
        (@Id, @Name, @HexCode, @IsActive, @IsDeleted, @CreatedDate, @Description, @Slug)";

                Guid colorId = Guid.NewGuid();

                using (var cmd = new NpgsqlCommand(addQuery, con))
                {
                    cmd.Parameters.AddWithValue("@Id", colorId);
                    cmd.Parameters.AddWithValue("@Name", request.Name);
                    cmd.Parameters.AddWithValue("@HexCode", request.HexCode);
                    cmd.Parameters.AddWithValue("@IsActive", request.IsActive);
                    cmd.Parameters.AddWithValue("@IsDeleted", request.IsDeleted);
                    cmd.Parameters.AddWithValue("@CreatedDate", DateTime.UtcNow);
                    cmd.Parameters.AddWithValue("@Description", request.Description);
                    cmd.Parameters.AddWithValue("@Slug", baseSlug);

                    await cmd.ExecuteNonQueryAsync();
                }

                return new
                {
                    Success = true,
                    Message = "Color added successfully",
                    ColorId = colorId
                };
            }
        }

        public async Task<object> GetColors()
        {
            using (var con = new NpgsqlConnection(DbConnection))
            {
                await con.OpenAsync();

                string getQuery = @"SELECT id, name, hexcode FROM colors ORDER BY id DESC";

                using (var cmd = new NpgsqlCommand(getQuery, con))
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        var colors = new List<object>();

                        while (await reader.ReadAsync())
                        {
                            colors.Add(new
                            {
                                id = reader["id"],
                                name = reader["name"],
                                hexcode = reader["hexcode"]
                            });
                        }

                        return new
                        {
                            success = true,
                            message = "Colors fetched successfully",
                            data = colors
                        };
                    }
                }
            }
        }

        public async Task<object> UpdateColor(string userEmail, Guid id, ProductsCollors request, string baseSlug)
        {
            using (var con = new NpgsqlConnection(DbConnection))
            {
                await con.OpenAsync();

                string updateQuery = @"UPDATE colors 
                               SET name = @Name,
                                   hexcode = @HexCode,
                                   isactive = @IsActive,
                                   isdeleted = @IsDeleted,
                                   description = @Description,
                                   slug = @Slug,
                                   createddate = @UpdatedDate
                               WHERE id = @Id";

                using (var cmd = new NpgsqlCommand(updateQuery, con))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@Name", request.Name);
                    cmd.Parameters.AddWithValue("@HexCode", request.HexCode);
                    cmd.Parameters.AddWithValue("@IsActive", request.IsActive);
                    cmd.Parameters.AddWithValue("@IsDeleted", request.IsDeleted);
                    cmd.Parameters.AddWithValue("@Description", request.Description);
                    cmd.Parameters.AddWithValue("@Slug", baseSlug);
                    cmd.Parameters.AddWithValue("@UpdatedDate", DateTime.UtcNow);

                    int rowsAffected = await cmd.ExecuteNonQueryAsync();

                    if (rowsAffected == 0)
                    {
                        return new
                        {
                            Success = false,
                            Message = "Color not found"
                        };
                    }

                    return new
                    {
                        Success = true,
                        Message = "Color updated successfully",
                        ColorId = id
                    };
                }
            }
        }

        public async Task<object> ToggleColorStatus(Guid id)
        {
            using (var con = new NpgsqlConnection(DbConnection))
            {
                await con.OpenAsync();

                string toggleQuery = @"
        UPDATE colors
        SET isactive = NOT isactive,
            createddate = @UpdatedDate
        WHERE id = @Id
        RETURNING id, isactive";

                using (var cmd = new NpgsqlCommand(toggleQuery, con))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@UpdatedDate", DateTime.UtcNow);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new
                            {
                                Success = true,
                                Message = "Color status toggled successfully",
                                Id = reader["id"],
                                IsActive = reader["isactive"]
                            };
                        }
                        else
                        {
                            return new
                            {
                                Success = false,
                                Message = "Color not found"
                            };
                        }
                    }
                }
            }
        }

        public async Task<object> SoftDeleteColor(Guid id)
        {
            using (var con = new NpgsqlConnection(DbConnection))
            {
                await con.OpenAsync();

                string deleteQuery = @"
        UPDATE colors
        SET isdeleted = true,
            createddate = @UpdatedDate
        WHERE id = @Id
        RETURNING id";

                using (var cmd = new NpgsqlCommand(deleteQuery, con))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@UpdatedDate", DateTime.UtcNow);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new
                            {
                                Success = true,
                                Message = "Color deleted successfully",
                                Id = reader["id"]
                            };
                        }
                        else
                        {
                            return new
                            {
                                Success = false,
                                Message = "Color not found"
                            };
                        }
                    }
                }
            }
        }

        public async Task<object> RestoreColor(Guid id)
        {
            using (var con = new NpgsqlConnection(DbConnection))
            {
                await con.OpenAsync();

                string query = @"UPDATE colors
                         SET isdeleted = false
                         WHERE id = @Id
                         RETURNING id";

                using (var cmd = new NpgsqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Id", id);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new
                            {
                                success = true,
                                message = "Color restored successfully",
                                id = reader["id"]
                            };
                        }
                    }
                }
            }

            return new
            {
                success = false,
                message = "Color not found"
            };
        }

        public async Task<object> DeleteColor(Guid id)
        {
            using (var con = new NpgsqlConnection(DbConnection))
            {
                await con.OpenAsync();

                string deleteQuery = @"DELETE FROM colors WHERE id = @Id RETURNING id";

                using (var cmd = new NpgsqlCommand(deleteQuery, con))
                {
                    cmd.Parameters.AddWithValue("@Id", id);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new
                            {
                                success = true,
                                message = "Color deleted permanently",
                                id = reader["id"]
                            };
                        }
                    }
                }
            }

            return new
            {
                success = false,
                message = "Color not found"
            };
        }
    }
}
