
using elemechWisetrack.Models;
using Microsoft.AspNetCore.Mvc;

namespace elemechWisetrack.BusinessLayer
{
    public interface IBusinessLayer_Categories
{
        Task<IActionResult> UploadCategory(IFormCollection form);
        Task<List<CategoryTreeDto>> GetCategoryTree();
        Task<CategoryTreeDto?> ListCategoryById(Guid categoryId);
        Task<IActionResult> UpdateCategory(Guid categoryId,IFormCollection form);
        Task<IActionResult> DeleteCategory(Guid categoryId);
    }



    public partial interface IBusinessLayer : IBusinessLayer_Categories { }

    public partial class BusinessLayer
    {

        public async Task<IActionResult> UploadCategory(IFormCollection form)
        {
            string name = form["category_name"];
            if (string.IsNullOrWhiteSpace(name))
                return new BadRequestObjectResult(new { success = false, message = "Category name is required" });

            // Generate slug
            string slug = GenerateSlug(name);

            // Add slug to form or pass separately to DAL
            return await _dataBaseLayer.UploadCategory(form, slug);
        }

        public async Task<List<CategoryTreeDto>> GetCategoryTree()
        {
            return await _dataBaseLayer.GetCategoryTree();
        }

        public async Task<CategoryTreeDto?> ListCategoryById(Guid categoryId)
        {
            return await _dataBaseLayer.ListCategoryById(categoryId);
        }

        public async Task<IActionResult> UpdateCategory(Guid categoryId, IFormCollection form)
        {
            string name = form["category_name"];
            if (string.IsNullOrWhiteSpace(name))
                return new BadRequestObjectResult(new { success = false, message = "Category name is required" });

            // Generate slug
            string slug = GenerateSlug(name);

            return await _dataBaseLayer.UpdateCategory(categoryId, form, slug);
        }

        public async Task<IActionResult> DeleteCategory(Guid categoryId)
        {
            return await _dataBaseLayer.DeleteCategory(categoryId);
        }

        public static string GenerateSlug(string input)
        {
            // 1. To lower
            string slug = input.ToLowerInvariant();

            // 2. Remove invalid chars
            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\s-]", "");

            // 3. Convert multiple spaces into one
            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"\s+", " ").Trim();

            // 4. Replace spaces with hyphens
            slug = slug.Replace(" ", "-");

            return slug;
        }

    }
}
