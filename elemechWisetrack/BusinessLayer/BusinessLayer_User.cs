using Microsoft.AspNetCore.Mvc;

namespace elemechWisetrack.BusinessLayer
{
    public interface iBusinessLayer_User
    {
        Task<object> AddWishListProduct(string productId,string email);
        Task<object> GetWishListProduct(string email);
        Task<object> DeleteWishListProduct(string productId, string email);
    }

    public partial interface IBusinessLayer : iBusinessLayer_User { }

    public partial class BusinessLayer
    {
        public async Task<object> AddWishListProduct(string productId, string email)
        {
            return await _dataBaseLayer.AddWishListProduct(productId, email);
        }

        public async Task<object> GetWishListProduct(string email)
        {
            return await _dataBaseLayer.GetWishListProduct(email);
        }

        public async Task<object> DeleteWishListProduct(string productId, string email)
        {
            return await _dataBaseLayer.DeleteWishListProduct(productId, email);
        }
    }
}
