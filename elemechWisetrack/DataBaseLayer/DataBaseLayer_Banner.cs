using elemechWisetrack.Models;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace elemechWisetrack.DataBaseLayer
{
    public interface IDataBaseLayer_Banner
    {
        Task<object> CreateBanner(CreateBannerDbModel model);
        Task<object> GetBanners();
        Task<object> GetBannerById(Guid id);
        Task<object> UpdateBanner(UpdateBannerDbModel model);
        Task<object> DeleteBanner(Guid id);
    }

    public partial interface IDataBaseLayer : IDataBaseLayer_Banner { }

    public partial class DataBaseLayer : IDataBaseLayer
    {
       
        public async Task<object> CreateBanner(CreateBannerDbModel model)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            string query = "INSERT INTO banners (title,image,link,is_active) VALUES (@t,@i,@l,true)";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@t", model.Title ?? "");
            cmd.Parameters.AddWithValue("@i", model.Image ?? "");
            cmd.Parameters.AddWithValue("@l", (object?)model.Link ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync();

            return new { message = "Banner created successfully" };
        }

        public async Task<object> GetBanners()
        {
            var list = new List<BannerModel>();

            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            var cmd = new NpgsqlCommand("SELECT id,title,image,link,is_active FROM banners", conn);
            var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new BannerModel
                {
                    Id = reader.GetGuid(0),
                    Title = reader.GetString(1),
                    Image = reader.GetString(2),

                    // ✅ FIX HERE
                    Link = reader.IsDBNull(3) ? null : reader.GetString(3),

                    IsActive = reader.GetBoolean(4)
                });
            }

            return list;
        }

        public async Task<object> GetBannerById(Guid id)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            var cmd = new NpgsqlCommand("SELECT id,title,image,link,is_active FROM banners WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("@id", id);

            var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new BannerModel
                {
                    Id = reader.GetGuid(0),
                    Title = reader.GetString(1),
                    Image = reader.GetString(2),
                    Link = reader.IsDBNull(3) ? null : reader.GetString(3),
                    IsActive = reader.GetBoolean(4)
                };
            }
            return null;
        }

        public async Task<object> UpdateBanner(UpdateBannerDbModel model)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            var query = @"UPDATE banners SET 
                          title=@t,
                          image=COALESCE(NULLIF(@i,''),image),
                          link=@l,
                          is_active=@a,
                          updated_at=NOW()
                          WHERE id=@id";

            var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", model.Id);
            cmd.Parameters.AddWithValue("@t", model.Title ?? "");
            cmd.Parameters.AddWithValue("@i", model.Image ?? "");
            cmd.Parameters.AddWithValue("@l", (object?)model.Link ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@a", model.IsActive);

            await cmd.ExecuteNonQueryAsync();

            return new { message = "Updated successfully" };
        }

        public async Task<object> DeleteBanner(Guid id)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            // Permanently remove the record from the database
            var cmd = new NpgsqlCommand("DELETE FROM banners WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("@id", id);

            var rowsAffected = await cmd.ExecuteNonQueryAsync();

            if (rowsAffected > 0)
                return new { message = "Banner deleted successfully" };
            else
                return new { message = "Banner not found" };
        }
    }
}