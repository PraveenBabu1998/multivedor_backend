using elemechWisetrack.Models;
using Microsoft.AspNetCore.Mvc;

namespace elemechWisetrack.BusinessLayer
{
    public interface IBusinessLayer_Brands
    {
        Task<object> AddBrands(string userEmail, [FromBody] BrandInsertModel request);
        Task<List<BrandModel>> GetBrands();
        Task<BrandModel> GetBrandById(Guid id);
        Task<object> UpdateBrandsById(Guid id, string userEmail, [FromBody] BrandInsertModel request);
        Task<object> DeleteBrandsById(Guid id);
        Task<object> ToggleBrandsById(Guid id);
    }

    public partial interface IBusinessLayer : IBusinessLayer_Brands { }

    public partial class BusinessLayer
    {
        public async Task<object> AddBrands(string userEmail, [FromBody] BrandInsertModel request)
        {
            string name = request.Name;
            if (string.IsNullOrWhiteSpace(name))
                return new BadRequestObjectResult(new { success = false, message = "Category name is required" });

            // Generate slug
            string slug = GenerateBrandSlug(name);
            return await _dataBaseLayer.AddBrands(userEmail, request,slug);
        }

        public async Task<List<BrandModel>> GetBrands()
        {
            return await _dataBaseLayer.GetBrands();
        }

        public async Task<BrandModel> GetBrandById(Guid id)
        {
            return await _dataBaseLayer.GetBrandById(id);
        }

        public async Task<object> UpdateBrandsById(Guid id, string userEmail, [FromBody] BrandInsertModel request)
        {
            return await _dataBaseLayer.UpdateBrandsById(id, userEmail, request);
        }

        public async Task<object> DeleteBrandsById(Guid id)
        {
            return await _dataBaseLayer.DeleteBrandsById(id);
        }

        public async Task<object> ToggleBrandsById(Guid id)
        {
            return await _dataBaseLayer.ToggleBrandsById(id);
        }

        public static string GenerateBrandSlug(string input)
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
