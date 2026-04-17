using elemechWisetrack.Models;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace elemechWisetrack.DataBaseLayer
{
    public interface IDataBaseLayer_Size
    {
        Task<object> AddSize(string userEmail, ProductSizes request, string slug);
        Task<object> GetSizes();
        Task<object> UpdateSize(Guid id, ProductSizes request, string slug);
        Task<object> ToggleSizeStatus(Guid id);
        Task<object> SoftDeleteSize(Guid id);
        Task<object> DeleteSize(Guid id);
    }

    public partial interface IDataBaseLayer : IDataBaseLayer_Size { }

    public partial class DataBaseLayer
    {
        public async Task<object> AddSize(string userEmail, ProductSizes request, string slug)
        {
            using (var con = new NpgsqlConnection(DbConnection))
            {
                await con.OpenAsync();

                string query = @"INSERT INTO sizes
        (id, name, description, slug, isactive, isdeleted, createddate)
        VALUES
        (@Id, @Name, @Description, @Slug, @IsActive, @IsDeleted, @CreatedDate)";

                Guid sizeId = Guid.NewGuid();

                using (var cmd = new NpgsqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Id", sizeId);
                    cmd.Parameters.AddWithValue("@Name", request.Name);
                    cmd.Parameters.AddWithValue("@Description", request.Description ?? "");
                    cmd.Parameters.AddWithValue("@Slug", slug);
                    cmd.Parameters.AddWithValue("@IsActive", request.IsActive);
                    cmd.Parameters.AddWithValue("@IsDeleted", request.IsDeleted);
                    cmd.Parameters.AddWithValue("@CreatedDate", DateTime.UtcNow);

                    await cmd.ExecuteNonQueryAsync();
                }

                return new { success = true, message = "Size added", id = sizeId };
            }
        }

        public async Task<object> GetSizes()
        {
            using (var con = new NpgsqlConnection(DbConnection))
            {
                await con.OpenAsync();

                string query = @"SELECT id,name,isactive
                         FROM sizes
                         WHERE isdeleted=false
                         ORDER BY createddate DESC";

                using (var cmd = new NpgsqlCommand(query, con))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    var list = new List<object>();

                    while (await reader.ReadAsync())
                    {
                        list.Add(new
                        {
                            id = reader["id"],
                            name = reader["name"],
                            isactive = reader["isactive"]
                        });
                    }

                    return new { success = true, data = list };
                }
            }
        }

        public async Task<object> UpdateSize(Guid id, ProductSizes request, string slug)
        {
            using (var con = new NpgsqlConnection(DbConnection))
            {
                await con.OpenAsync();

                string query = @"UPDATE sizes
                         SET name=@Name,
                             description=@Description,
                             slug=@Slug,
                             updateddate=@UpdatedDate
                         WHERE id=@Id";

                using (var cmd = new NpgsqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@Name", request.Name);
                    cmd.Parameters.AddWithValue("@Description", request.Description ?? "");
                    cmd.Parameters.AddWithValue("@Slug", slug);
                    cmd.Parameters.AddWithValue("@UpdatedDate", DateTime.UtcNow);

                    await cmd.ExecuteNonQueryAsync();
                }

                return new { success = true, message = "Size updated" };
            }
        }

        public async Task<object> ToggleSizeStatus(Guid id)
        {
            using (var con = new NpgsqlConnection(DbConnection))
            {
                await con.OpenAsync();

                string query = @"UPDATE sizes
                         SET isactive = NOT isactive
                         WHERE id=@Id
                         RETURNING isactive";

                using (var cmd = new NpgsqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Id", id);

                    var status = await cmd.ExecuteScalarAsync();

                    return new { success = true, isactive = status };
                }
            }
        }

        public async Task<object> SoftDeleteSize(Guid id)
        {
            using (var con = new NpgsqlConnection(DbConnection))
            {
                await con.OpenAsync();

                string query = @"UPDATE sizes
                         SET isdeleted=true
                         WHERE id=@Id";

                using (var cmd = new NpgsqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    await cmd.ExecuteNonQueryAsync();
                }

                return new { success = true, message = "Size deleted" };
            }
        }

        public async Task<object> DeleteSize(Guid id)
        {
            using (var con = new NpgsqlConnection(DbConnection))
            {
                await con.OpenAsync();

                string query = @"DELETE FROM sizes 
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
                                message = "Size deleted permanently",
                                id = reader["id"]
                            };
                        }
                    }
                }
            }

            return new
            {
                success = false,
                message = "Size not found"
            };
        }

    }
}
