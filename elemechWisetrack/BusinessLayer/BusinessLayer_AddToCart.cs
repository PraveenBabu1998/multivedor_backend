using elemechWisetrack.Models;

namespace elemechWisetrack.BusinessLayer
{
    public interface IBusinessLayer_AddToCart
    {
        Task<object> AddToCart(string userEmail, AddToCartModel model);
        Task<object> GetCart(string userEmail);
        Task<object> UpdateCart(string userEmail, UpdateCartModel model);
        Task<object> RemoveItem(string userEmail, Guid productId);
        Task<object> ClearCart(string userEmail);
    }

    public partial interface IBusinessLayer : IBusinessLayer_AddToCart { }

    public partial class BusinessLayer
    {
        public async Task<object> AddToCart(string userEmail, AddToCartModel model)
        {
            return await _dataBaseLayer.AddToCart(userEmail, model);
        }

        public async Task<object> GetCart(string userEmail)
        {
            return await _dataBaseLayer.GetCart(userEmail);
        }

        public async Task<object> UpdateCart(string userEmail, UpdateCartModel model)
        {
            return await _dataBaseLayer.UpdateCart(userEmail, model);
        }

        public async Task<object> RemoveItem(string userEmail, Guid productId)
        {
            return await _dataBaseLayer.RemoveItem(userEmail, productId);
        }

        public async Task<object> ClearCart(string userEmail)
        {
            return await _dataBaseLayer.ClearCart(userEmail);
        }
    }
}
