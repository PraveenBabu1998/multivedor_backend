using Microsoft.AspNetCore.Mvc;

namespace elemechWisetrack.BusinessLayer
{
    public interface iBusinessLayer_User
    {
        Task<object> AddWishListProduct(string productId, string email, string ipAddress);
        Task<object> GetWishListProduct(string email, string ipAddress);
        Task<object> DeleteWishListProduct(string productId, string email, string ipAddress);
    }

    public partial interface IBusinessLayer : iBusinessLayer_User { }

    public partial class BusinessLayer
    {
        public async Task<object> AddWishListProduct(string productId, string email, string ipAddress)
        {
            return await _dataBaseLayer.AddWishListProduct(productId, email, ipAddress);
        }

        public async Task<object> GetWishListProduct(string email, string ipAddress)
        {
            return await _dataBaseLayer.GetWishListProduct(email, ipAddress);
        }

        public async Task<object> DeleteWishListProduct(string productId, string email, string ipAddress)
        {
            return await _dataBaseLayer.DeleteWishListProduct(productId, email, ipAddress);
        }
    }
}
