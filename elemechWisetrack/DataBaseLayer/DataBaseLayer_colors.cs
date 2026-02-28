using elemechWisetrack.Models;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace elemechWisetrack.DataBaseLayer
{
    public interface IDataBaseLayer_colors
    {
        Task<object> AddColors([FromBody] ProductsCollors request, string baseSlug);
    }

    public partial interface IDataBaseLayer : IDataBaseLayer_colors
    {

    }

    public partial class DataBaseLayer
    {
        public async Task<object> AddColors([FromBody] ProductsCollors request, string baseSlug)
        {
            using (var con = new NpgsqlConnection(DbConnection))
            {
                await con.OpenAsync();

                string addQuery = @"INSERT INTO colors (id, name, hexcode, isactive, isdeleted, createddate, description, slug) VALUE 
                                    (@Id, @Name, @HexCode, @IsActive, @IsDeleted, @CreatedDate, @Description, @Slug )";

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
                    cmd.Parameters.AddWithValue("@Slug", request.Slug);

                    await cmd.ExecuteNonQueryAsync();
                }

                return new
                {
                    Success = true,
                    Message = "Brand added successfully",
                    BrandId = colorId
                };
            }
        }
    }
}
