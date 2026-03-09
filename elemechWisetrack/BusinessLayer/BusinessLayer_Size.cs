using elemechWisetrack.Models;

namespace elemechWisetrack.BusinessLayer
{
    public interface IBusinessLayer_Size
    {
        Task<object> AddSize(string userEmail, ProductSizes request);
        Task<object> GetSizes();
        Task<object> UpdateSize(Guid id, ProductSizes request);
        Task<object> ToggleSizeStatus(Guid id);
        Task<object> SoftDeleteSize(Guid id);
        Task<object> RestoreSize(Guid id);
        Task<object> DeleteSize(Guid id);
        Task<object> AddProductSize(ProductSizeRequest request);
        Task<object> GetProductSizes();
        Task<object> GetSizeByProduct(Guid productId);
        Task<object> DeleteProductSize(Guid id);
        Task<object> UpdateProductSize(Guid id, ProductSizeRequest request);
    }

    public partial interface IBusinessLayer : IBusinessLayer_Size { }

    public partial class BusinessLayer
    {
        public async Task<object> AddSize(string userEmail, ProductSizes request)
        {
            string slug = GenerateSlugProduct(request.Name);
            return await _dataBaseLayer.AddSize(userEmail, request, slug);
        }

        public async Task<object> GetSizes()
        {
            return await _dataBaseLayer.GetSizes();
        }

        public async Task<object> UpdateSize(Guid id, ProductSizes request)
        {
            string slug = GenerateSlugProduct(request.Name);
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

        public async Task<object> RestoreSize(Guid id)
        {
            return await _dataBaseLayer.RestoreSize(id);
        }

        public async Task<object> DeleteSize(Guid id)
        {
            return await _dataBaseLayer.DeleteSize(id);
        }

        public async Task<object> AddProductSize(ProductSizeRequest request)
        {
            return await _dataBaseLayer.AddProductSize(request);
        }

        public async Task<object> GetProductSizes()
        {
            return await _dataBaseLayer.GetProductSizes();
        }

        public async Task<object> GetSizeByProduct(Guid productId)
        {
            return await _dataBaseLayer.GetSizeByProduct(productId);
        }

        public async Task<object> DeleteProductSize(Guid id)
        {
            return await _dataBaseLayer.DeleteProductSize(id);
        }

        public async Task<object> UpdateProductSize(Guid id, ProductSizeRequest request)
        {
            return await _dataBaseLayer.UpdateProductSize(id, request);
        }
    }
}
