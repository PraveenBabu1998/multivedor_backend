using elemechWisetrack.DataBaseLayer;
using elemechWisetrack.Models;

namespace elemechWisetrack.BusinessLayer
{
    public interface IBusinessLayer_AddToCart
    {
        Task<object> AddToCart(string email, string ip, AddToCartModel model);
        Task<object> GetCart(string email, string ip);
        Task<object> UpdateCart(string email, string ip, UpdateCartModel model);
        Task<object> RemoveItem(string email, string ip, Guid productId);
        Task<object> ClearCart(string email, string ip);
    }

    public partial interface IBusinessLayer : IBusinessLayer_AddToCart { }

    public partial class BusinessLayer
    {
        

        public Task<object> AddToCart(string email, string ip, AddToCartModel model)
            => _dataBaseLayer.AddToCart(email, ip, model);

        public Task<object> GetCart(string email, string ip)
            => _dataBaseLayer.GetCart(email, ip);

        public Task<object> UpdateCart(string email, string ip, UpdateCartModel model)
            => _dataBaseLayer.UpdateCart(email, ip, model);

        public Task<object> RemoveItem(string email, string ip, Guid productId)
            => _dataBaseLayer.RemoveItem(email, ip, productId);

        public Task<object> ClearCart(string email, string ip)
            => _dataBaseLayer.ClearCart(email, ip);
    }
}