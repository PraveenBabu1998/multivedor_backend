using CareerCracker.S3Services;
using elemechWisetrack.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Npgsql;
using System.Buffers.Text;

namespace elemechWisetrack.DataBaseLayer
{
    public interface IDataBaseLayer_Categories
    {
        Task<IActionResult> UploadCategory(IFormCollection form, string slug);
        Task<List<CategoryTreeDto>> GetCategoryTree();
        Task<CategoryTreeDto?> ListCategoryById(Guid categoryId);
        Task<IActionResult> UpdateCategory(Guid categoryId, IFormCollection form,string slug);
        Task<IActionResult> DeleteCategory(Guid categoryId);
    }

    public partial interface IDataBaseLayer : IDataBaseLayer_Categories
    {
    }

    public partial class DataBaseLayer : IDataBaseLayer
    {
        public async Task<IActionResult> UploadCategory(IFormCollection form, string slug)
        {
            string categoryName = form["category_name"];
            bool categoryStatus = Convert.ToBoolean(form["category_status"]);
            string? imageFileName = null;
            string? parentIdValue = form["parent_id"];

            Guid? parentId = null;

            if (!string.IsNullOrEmpty(parentIdValue) && Guid.TryParse(parentIdValue, out Guid parsedId))
            {
                parentId = parsedId;
            }


            var imageFile = form.Files["category_image"];
            if (imageFile != null && imageFile.Length > 0)
            {
                imageFileName = await S3StorageHelper.UploadFileAsync(imageFile, "uploads/categories");
            }

            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            string sql = @"
        INSERT INTO categories 
        (id, name, slug, parentid, image, isactive)
        VALUES 
        (@id, @name, @slug, @parentid, @image, @isactive);
    ";

            using var cmd = new NpgsqlCommand(sql, conn);

            cmd.Parameters.AddWithValue("id", Guid.NewGuid());
            cmd.Parameters.AddWithValue("name", categoryName);
            cmd.Parameters.AddWithValue("slug", slug);
            cmd.Parameters.AddWithValue("parentid", (object?)parentId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("image", (object?)imageFileName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("isactive", categoryStatus);

            await cmd.ExecuteNonQueryAsync();

            return new OkObjectResult(new
            {
                success = true,
                message = "Category uploaded successfully"
            });
        }

        // UploadCategory method here...

        public async Task<List<CategoryTreeDto>> GetCategoryTree()
        {
            var categories = new List<CategoryTreeDto>();

            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            string sql = @"SELECT id, name, slug, parentid, image, isactive FROM categories;";

            using var cmd = new NpgsqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();


            while (await reader.ReadAsync())
            {
                var imageName = reader.IsDBNull(4) ? null : reader.GetString(4);

                categories.Add(new CategoryTreeDto
                {
                    Id = reader.GetGuid(0),
                    Name = reader.GetString(1),
                    Slug = reader.GetString(2),
                    ParentId = reader.IsDBNull(3) ? null : reader.GetGuid(3),
                    Image = CategoryImagePublicUrl(imageName),
                    IsActive = reader.GetBoolean(5),
                    Children = new List<CategoryTreeDto>()
                });
            }

            return BuildTree(categories);
        }

        public async Task<CategoryTreeDto?> ListCategoryById(Guid categoryId)
        {
            var allCategories = new List<CategoryTreeDto>();

            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            string sql = @"SELECT id, name, slug, parentid, image, isactive FROM categories;";

            using var cmd = new NpgsqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var imageName = reader.IsDBNull(4) ? null : reader.GetString(4);

                allCategories.Add(new CategoryTreeDto
                {
                    Id = reader.GetGuid(0),
                    Name = reader.GetString(1),
                    Slug = reader.GetString(2),
                    ParentId = reader.IsDBNull(3) ? null : reader.GetGuid(3),

                    Image = CategoryImagePublicUrl(imageName),

                    IsActive = reader.GetBoolean(5),
                    Children = new List<CategoryTreeDto>()
                });
            }

            var tree = BuildTree(allCategories);

            return FindCategory(tree, categoryId);
        }

        private CategoryTreeDto? FindCategory(List<CategoryTreeDto> categories, Guid id)
        {
            foreach (var category in categories)
            {
                if (category.Id == id)
                    return category;

                var found = FindCategory(category.Children, id);
                if (found != null)
                    return found;
            }

            return null;
        }

        public async Task<IActionResult> UpdateCategory(
    Guid categoryId,
    IFormCollection form,
    string slug)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            // 1️⃣ Check if category exists
            string checkQuery = "SELECT image FROM categories WHERE id = @id;";
            string? existingImage = null;

            using (var checkCmd = new NpgsqlCommand(checkQuery, conn))
            {
                checkCmd.Parameters.AddWithValue("id", categoryId);

                using var reader = await checkCmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    return new NotFoundObjectResult(new
                    {
                        success = false,
                        message = "Category not found"
                    });
                }

                existingImage = reader.IsDBNull(0) ? null : reader.GetString(0);
            }

            // 2️⃣ Read form values
            string categoryName = form["category_name"];
            bool categoryStatus = Convert.ToBoolean(form["category_status"]);
            string? parentIdValue = form["parent_id"];

            Guid? parentId = null;

            if (!string.IsNullOrEmpty(parentIdValue) &&
                Guid.TryParse(parentIdValue, out Guid parsedId))
            {
                parentId = parsedId;
            }

            // ❌ Prevent self-parent
            if (parentId == categoryId)
            {
                return new BadRequestObjectResult(new
                {
                    success = false,
                    message = "Category cannot be its own parent"
                });
            }

            // 3️⃣ Handle Image Upload
            string? imageFileName = existingImage;

            var imageFile = form.Files["category_image"];

            if (imageFile != null && imageFile.Length > 0)
            {
                await S3StorageHelper.DeleteStoredMediaAsync(existingImage);
                imageFileName = await S3StorageHelper.UploadFileAsync(imageFile, "uploads/categories");
            }

            // 4️⃣ Update Query
            string updateQuery = @"
        UPDATE categories
        SET name = @name,
            slug = @slug,
            parentid = @parentid,
            image = @image,
            isactive = @isactive
        WHERE id = @id;
    ";

            using var cmd = new NpgsqlCommand(updateQuery, conn);

            cmd.Parameters.AddWithValue("id", categoryId);
            cmd.Parameters.AddWithValue("name", categoryName);
            cmd.Parameters.AddWithValue("slug", slug);
            cmd.Parameters.AddWithValue("parentid", (object?)parentId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("image", (object?)imageFileName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("isactive", categoryStatus);

            await cmd.ExecuteNonQueryAsync();

            return new OkObjectResult(new
            {
                success = true,
                message = "Category updated successfully"
            });
        }

        public async Task<IActionResult> DeleteCategory(Guid categoryId)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            // 1️⃣ Check if category exists
            string checkQuery = "SELECT COUNT(1) FROM categories WHERE id = @id;";
            using (var checkCmd = new NpgsqlCommand(checkQuery, conn))
            {
                checkCmd.Parameters.AddWithValue("id", categoryId);
                var exists = (long)await checkCmd.ExecuteScalarAsync();

                if (exists == 0)
                {
                    return new NotFoundObjectResult(new
                    {
                        Status = false,
                        Message = "Category not found"
                    });
                }
            }

            // 2️⃣ Check if category has children
            string childCheckQuery = "SELECT COUNT(1) FROM categories WHERE parentid = @id;";
            using (var childCmd = new NpgsqlCommand(childCheckQuery, conn))
            {
                childCmd.Parameters.AddWithValue("id", categoryId);
                var childCount = (long)await childCmd.ExecuteScalarAsync();

                if (childCount > 0)
                {
                    return new BadRequestObjectResult(new
                    {
                        Status = false,
                        Message = "Cannot delete category because it has subcategories"
                    });
                }
            }

            // 3️⃣ Delete category
            string deleteQuery = "DELETE FROM categories WHERE id = @id;";
            using (var deleteCmd = new NpgsqlCommand(deleteQuery, conn))
            {
                deleteCmd.Parameters.AddWithValue("id", categoryId);
                await deleteCmd.ExecuteNonQueryAsync();
            }

            return new OkObjectResult(new
            {
                Status = true,
                Message = "Category deleted successfully"
            });
        }


        #region Build Tree (O(n))

        private static string? CategoryImagePublicUrl(string? imageValue)
        {
            if (string.IsNullOrEmpty(imageValue)) return null;
            if (imageValue.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                imageValue.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return imageValue;
            return $"uploads/categories/{imageValue}";
        }

        private List<CategoryTreeDto> BuildTree(List<CategoryTreeDto> categories)
            {
                var lookup = categories.ToDictionary(c => c.Id);
                var root = new List<CategoryTreeDto>();

                foreach (var category in categories)
                {
                    if (category.ParentId == null)
                    {
                        root.Add(category);
                    }
                    else if (lookup.ContainsKey(category.ParentId.Value))
                    {
                        lookup[category.ParentId.Value].Children.Add(category);
                    }
                }

                return root;
            }

            #endregion
        

    }
}