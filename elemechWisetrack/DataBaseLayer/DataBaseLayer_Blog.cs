using CareerCracker.S3Services;
using elemechWisetrack.Models;
using Humanizer;
using Npgsql;
using System.Text.RegularExpressions;

namespace elemechWisetrack.DataBaseLayer
{
    public interface IDataBaseLayer_Blog
    {
        Task<object> AddBlog(AddBlogDto model, string userId);
        Task<object> GetBlogs();
        Task<object> GetBlogById(string id);
        Task<object> UpdateBlog(string id, AddBlogDto model, string userId);
        Task<object> DeleteBlog(string id, string userId);
    }

    public partial interface IDataBaseLayer : IDataBaseLayer_Blog { }

    public partial class DataBaseLayer
    {
        private string GenerateSlug(string title)
        {
            string slug = title.ToLower();
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
            slug = Regex.Replace(slug, @"\s+", " ").Trim();
            return slug.Replace(" ", "-");
        }

        public async Task<object> AddBlog(AddBlogDto model, string userId)
        {
            using (var con = new NpgsqlConnection(DbConnection))
            {
                await con.OpenAsync();

                var slug = GenerateSlug(model.Title);

                string getUserQuery = @"
                SELECT ""Id""
                FROM ""AspNetUsers""
                WHERE ""Email"" = @Email
                LIMIT 1;";

                Guid userIds;

                using (var userCmd = new NpgsqlCommand(getUserQuery, con))
                {
                    userCmd.Parameters.AddWithValue("@Email", userId);

                    var result = await userCmd.ExecuteScalarAsync();

                    if (result == null)
                    {
                        return new
                        {
                            Success = false,
                            Message = "User not found"
                        };
                    }

                    userIds = Guid.Parse(result.ToString());
                }

                // ✅ STEP 1: Handle Image Upload
                string? imagePath = null;

                if (model.Image != null)
                {
                    imagePath = await S3StorageHelper.UploadFileAsync(model.Image, "uploads/blogs");
                }

                string query = @"
        INSERT INTO blogs 
        (user_id, title, slug, description, content, image, meta_title, meta_description, tags)
        VALUES 
        (@user_id, @title, @slug, @description, @content, @image, @meta_title, @meta_description, @tags)
        RETURNING id;
        ";

                using var cmd = new NpgsqlCommand(query, con);

                cmd.Parameters.AddWithValue("@user_id", userIds);
                cmd.Parameters.AddWithValue("@title", model.Title);
                cmd.Parameters.AddWithValue("@slug", slug);
                cmd.Parameters.AddWithValue("@description", model.Description);
                cmd.Parameters.AddWithValue("@content", model.Content ?? "");

                // ✅ FIX: store string path, NOT file
                cmd.Parameters.AddWithValue("@image", (object?)imagePath ?? DBNull.Value);

                cmd.Parameters.AddWithValue("@meta_title", (object?)model.MetaTitle ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@meta_description", (object?)model.MetaDescription ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@tags", model.Tags?.ToArray() ?? new string[] { });

                var id = await cmd.ExecuteScalarAsync();

                return new { success = true, blog_id = id };
            }
        }

        public async Task<object> GetBlogs()
        {
            using (var con = new NpgsqlConnection(DbConnection))
            {
                await con.OpenAsync();

                string query = "SELECT * FROM blogs ORDER BY created_at DESC";

                using var cmd = new NpgsqlCommand(query, con);
                using var reader = await cmd.ExecuteReaderAsync();

                var list = new List<object>();

                while (await reader.ReadAsync())
                {
                    list.Add(new
                    {
                        id = reader["id"],
                        user_id = reader["user_id"],
                        title = reader["title"],
                        slug = reader["slug"],
                        description = reader["description"],
                        content = reader["content"],
                        image = reader["image"] == DBNull.Value ? null : reader["image"], // ✅ only path
                        meta_title = reader["meta_title"] == DBNull.Value ? null : reader["meta_title"],
                        meta_description = reader["meta_description"] == DBNull.Value ? null : reader["meta_description"],
                        tags = reader["tags"] == DBNull.Value ? null : reader["tags"],
                        is_active = reader["is_active"],
                        created_at = reader["created_at"],
                        updated_at = reader["updated_at"]
                    });
                }

                return list;
            }
        }

        public async Task<object> GetBlogById(string id)
        {
            using (var con = new NpgsqlConnection(DbConnection))
            {
                await con.OpenAsync();

                string query = "SELECT * FROM blogs WHERE id = @id";

                using var cmd = new NpgsqlCommand(query, con);
                cmd.Parameters.AddWithValue("@id", Guid.Parse(id));

                using var reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return new
                    {
                        id = reader["id"],
                        user_id = reader["user_id"],
                        title = reader["title"],
                        slug = reader["slug"],
                        description = reader["description"],
                        content = reader["content"],
                        image = reader["image"] == DBNull.Value ? null : reader["image"], // ✅ only path
                        meta_title = reader["meta_title"] == DBNull.Value ? null : reader["meta_title"],
                        meta_description = reader["meta_description"] == DBNull.Value ? null : reader["meta_description"],
                        tags = reader["tags"] == DBNull.Value ? null : reader["tags"],
                        is_active = reader["is_active"],
                        created_at = reader["created_at"],
                        updated_at = reader["updated_at"]
                    };
                }

                return new { message = "Not found" };
            }
        }

        public async Task<object> UpdateBlog(string id, AddBlogDto model, string userEmail)
        {
            using (var con = new NpgsqlConnection(DbConnection))
            {
                await con.OpenAsync();

                // 🔹 STEP 1: Get User Guid
                string getUserQuery = @"
        SELECT ""Id""
        FROM ""AspNetUsers""
        WHERE ""Email"" = @Email
        LIMIT 1;";

                Guid userGuid;

                using (var userCmd = new NpgsqlCommand(getUserQuery, con))
                {
                    if (string.IsNullOrEmpty(userEmail))
                    {
                        return new { success = false, message = "User email is missing" };
                    }

                    userCmd.Parameters.AddWithValue("@Email", userEmail);

                    var result = await userCmd.ExecuteScalarAsync();

                    if (result == null)
                    {
                        return new { success = false, message = "User not found" };
                    }

                    userGuid = Guid.Parse(result.ToString());
                }

                // 🔹 STEP 2: Convert Status → is_active
                bool isActive = model.Status?.ToLower() == "true";

                // 🔹 STEP 3: Get Existing Image (IMPORTANT 🔥)
                string getImageQuery = "SELECT image FROM blogs WHERE id = @id";
                string imagePath = null;

                using (var imgCmd = new NpgsqlCommand(getImageQuery, con))
                {
                    imgCmd.Parameters.AddWithValue("@id", Guid.Parse(id));
                    var existingImage = await imgCmd.ExecuteScalarAsync();
                    imagePath = existingImage?.ToString();
                }

                // 🔹 STEP 4: Upload New Image (if provided)
                if (model.Image != null)
                {
                    await S3StorageHelper.DeleteStoredMediaAsync(imagePath);
                    imagePath = await S3StorageHelper.UploadFileAsync(model.Image, "uploads/blogs");
                }

                // 🔹 STEP 5: Update Query
                string query = @"
        UPDATE blogs SET 
            title = @title,
            description = @description,
            content = @content,
            is_active = @is_active,
            image = @image,
            updated_at = NOW()
        WHERE id = @id AND user_id = @user_id;
        ";

                using var cmd = new NpgsqlCommand(query, con);

                cmd.Parameters.AddWithValue("@id", Guid.Parse(id));
                cmd.Parameters.AddWithValue("@user_id", userGuid);
                cmd.Parameters.AddWithValue("@title", model.Title ?? "");
                cmd.Parameters.AddWithValue("@description", model.Description ?? "");
                cmd.Parameters.AddWithValue("@content", model.Content ?? "");
                cmd.Parameters.AddWithValue("@is_active", isActive);
                cmd.Parameters.AddWithValue("@image", (object?)imagePath ?? DBNull.Value);

                var rows = await cmd.ExecuteNonQueryAsync();

                return new
                {
                    success = rows > 0,
                    message = rows > 0 ? "Blog updated successfully" : "No blog updated"
                };
            }
        }

        public async Task<object> DeleteBlog(string id, string userId)
        {
            using (var con = new NpgsqlConnection(DbConnection))
            {
                await con.OpenAsync();

                string query = "DELETE FROM blogs WHERE id=@id ";

                using var cmd = new NpgsqlCommand(query, con);
                cmd.Parameters.AddWithValue("@id", Guid.Parse(id));

                var rows = await cmd.ExecuteNonQueryAsync();

                return new { success = rows > 0 };
            }
        }
    }
}

