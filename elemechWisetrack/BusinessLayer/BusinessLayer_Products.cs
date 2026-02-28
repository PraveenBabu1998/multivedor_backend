using elemechWisetrack.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace elemechWisetrack.BusinessLayer
{
    public interface IBusinessLayer_Products
    {
        Task<object> AddSingleProduct(string userEmail, [FromBody] ProductInsertModel request);
        Task<object> GetAllProducts(int? page, int? pageSize);
        Task<object> UpdateProduct(
    Guid productId,
    string userEmail,
    ProductInsertModel request);
    }

    public partial interface IBusinessLayer : IBusinessLayer_Products
    {

    }

    public partial class BusinessLayer
    {
        public async Task<object> AddSingleProduct(string userEmail, [FromBody] ProductInsertModel request)
        {
            if (string.IsNullOrEmpty(request.Name))
            {
                return "Product Name is required";
            }

            if (request.CategoryId == Guid.Empty)
            {
                // CategoryId is empty (00000000-0000-0000-0000-000000000000)
                return "Category id is required";
            }

            if (request.BrandId == Guid.Empty)
            {
                return "Brand is required";
            }

            if (request.Price <= 0)
            {
                return "Price is required";
            }

            string baseSlug = GenerateSlugProduct(request.Name);

            return await _dataBaseLayer.AddSingleProduct(userEmail, request, baseSlug);
        
        
        
        }

        public async Task<object> GetAllProducts(int? page, int? pageSize)
        {
            return await _dataBaseLayer.GetAllProducts(page, pageSize);
        }

        public async Task<object> UpdateProduct(
    Guid productId,
    string userEmail,
    ProductInsertModel request)
        {
            string baseSlug = GenerateSlugProduct(request.Name);
            return await _dataBaseLayer.UpdateProduct(productId, userEmail, request, baseSlug);
        }

        public string GenerateSlugProduct(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            // Convert to lowercase
            string slug = name.ToLower();

            // Remove special characters
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");

            // Replace multiple spaces with single space
            slug = Regex.Replace(slug, @"\s+", " ").Trim();

            // Replace spaces with hyphen
            slug = slug.Replace(" ", "-");

            return slug;
        }
    }
}
