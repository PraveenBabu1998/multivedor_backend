using elemechWisetrack.Models;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace elemechWisetrack.DataBaseLayer
{
    public interface IDataBaseLayer_colors
    {
        Task<object> AddColors(string userEmail,[FromBody] ProductsCollors request, string baseSlug);
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
    }
}
