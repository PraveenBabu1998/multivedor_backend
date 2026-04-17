using elemechWisetrack.Models;
using System.Text.RegularExpressions;

namespace elemechWisetrack.BusinessLayer
{
    public interface IBusinessLayer_Size
    {
        Task<object> AddSize(string userEmail, ProductSizes request);
        Task<object> GetSizes();
        Task<object> UpdateSize(Guid id, ProductSizes request);
        Task<object> ToggleSizeStatus(Guid id);
        Task<object> SoftDeleteSize(Guid id);
        Task<object> DeleteSize(Guid id);
    
    }

    public partial interface IBusinessLayer : IBusinessLayer_Size { }

    public partial class BusinessLayer
    {
        public async Task<object> AddSize(string userEmail, ProductSizes request)
        {
            string slug = CreateSizeSlug(request.Name);
            return await _dataBaseLayer.AddSize(userEmail, request, slug);
        }

        public async Task<object> GetSizes()
        {
            return await _dataBaseLayer.GetSizes();
        }

        public async Task<object> UpdateSize(Guid id, ProductSizes request)
        {
            string slug = CreateSizeSlug(request.Name);
            return await _dataBaseLayer.UpdateSize(id, request, slug);
        }

        public async Task<object> ToggleSizeStatus(Guid id)
        {
            return await _dataBaseLayer.ToggleSizeStatus(id);
        }

        public async Task<object> SoftDeleteSize(Guid id)
        {
            return await _dataBaseLayer.SoftDeleteSize(id);
        }
       
        public async Task<object> DeleteSize(Guid id)
        {
            return await _dataBaseLayer.DeleteSize(id);
        }

        private static string CreateSizeSlug(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Guid.NewGuid().ToString("N");
            }

            string slug = name.Trim().ToLowerInvariant();
            slug = Regex.Replace(slug, @"\s+", "-");
            slug = Regex.Replace(slug, @"[^a-z0-9\-]", string.Empty);
            slug = Regex.Replace(slug, @"\-{2,}", "-").Trim('-');

            return string.IsNullOrWhiteSpace(slug) ? Guid.NewGuid().ToString("N") : slug;
        }

    }
}
