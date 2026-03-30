using elemechWisetrack.Models;
using Npgsql;

namespace elemechWisetrack.DataBaseLayer
{
    public interface IDataBaseLayer_Articals
    {
        Task<object> AddArtical(string email, ArticalModel model);
        Task<object> GetAllArticals();
        Task<object> GetArticalById(Guid id);
        Task<object> UpdateArtical(string email, Guid id, ArticalModel model);
        Task<object> DeleteArtical(string email, Guid id);
    }

    public partial interface IDataBaseLayer : IDataBaseLayer_Articals { }

    public partial class DataBaseLayer
    {
        public async Task<object> AddArtical(string email, ArticalModel model)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            string query = @"INSERT INTO articles 
(title, slug, description, content, image_url, created_by)
VALUES (@title, @slug, @desc, @content, @img, @created_by)";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@title", model.Title);
            cmd.Parameters.AddWithValue("@slug", model.Slug);
            cmd.Parameters.AddWithValue("@desc", model.Description ?? "");
            cmd.Parameters.AddWithValue("@content", model.Content ?? "");
            cmd.Parameters.AddWithValue("@img", model.ImageUrl ?? "");
            cmd.Parameters.AddWithValue("@created_by", email ?? "");

            await cmd.ExecuteNonQueryAsync();

            return new { message = "Article added successfully" };
        }

        public async Task<object> GetAllArticals()
        {
            var list = new List<ArticalModel>();

            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            string query = "SELECT * FROM articles WHERE is_deleted=false ORDER BY created_at DESC";

            using var cmd = new NpgsqlCommand(query, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new ArticalModel
                {
                    Id = reader.GetGuid(0),
                    Title = reader.GetString(1),
                    Slug = reader.GetString(2),
                    Description = reader.GetString(3),
                    Content = reader.GetString(4),
                    ImageUrl = reader.GetString(5)
                });
            }

            return list;
        }

        public async Task<object> GetArticalById(Guid id)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            string query = "SELECT * FROM articles WHERE id=@id AND is_deleted=false";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new ArticalModel
                {
                    Id = reader.GetGuid(0),
                    Title = reader.GetString(1),
                    Slug = reader.GetString(2),
                    Description = reader.GetString(3),
                    Content = reader.GetString(4),
                    ImageUrl = reader.GetString(5)
                };
            }

            return null;
        }

        public async Task<object> UpdateArtical(string email, Guid id, ArticalModel model)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            string query = @"UPDATE articles 
SET title=@title, slug=@slug, description=@desc, content=@content, image_url=@img, updated_at=NOW()
WHERE id=@id";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@title", model.Title);
            cmd.Parameters.AddWithValue("@slug", model.Slug);
            cmd.Parameters.AddWithValue("@desc", model.Description ?? "");
            cmd.Parameters.AddWithValue("@content", model.Content ?? "");
            cmd.Parameters.AddWithValue("@img", model.ImageUrl ?? "");

            await cmd.ExecuteNonQueryAsync();

            return new { message = "Article updated successfully" };
        }

        public async Task<object> DeleteArtical(string email, Guid id)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            string query = "UPDATE articles SET is_deleted=true WHERE id=@id";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);

            await cmd.ExecuteNonQueryAsync();

            return new { message = "Article deleted successfully" };
        }
    }
}