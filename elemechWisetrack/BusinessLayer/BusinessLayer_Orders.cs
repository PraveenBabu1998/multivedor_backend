using elemechWisetrack.Models;
using Microsoft.AspNetCore.Mvc;

namespace elemechWisetrack.BusinessLayer
{
    public interface IBusinessLayer_Orders
    {
        Task<object> GetCheckoutDetails(string email, string couponCode);
        Task<object> CreateOrder(string email, CreateOrderModel model);
        Task<object> VerifyPayment(RazorpayVerifyModel model);
        Task<object> GetUserOrders(string email);
        Task<object> CancelOrder(string email, Guid orderId);
        Task<object> RequestExchange(string email, ExchangeRequestModel model);
        Task<object> GetExchangeRequests(string email, bool isAdmin);
        Task<object> UpdateExchangeStatus(UpdateExchangeStatusModel model);
        Task<object> SchedulePickup(PickupRequestModel model);
        Task<object> CompleteExchange(Guid exchangeId);
        Task<object> UpdatePickupStatus(Guid exchangeId, string status);
        Task<object> GetMyOrders(string email);
        Task<object> UpdateOrderStatus(UpdateOrderStatusModel model);
        Task<object> TrackOrder(Guid orderId);
        Task<object> GetOrderDetails(string email, Guid orderId);
    }

    public partial interface IBusinessLayer : IBusinessLayer_Orders
    {

    }

    public partial class BusinessLayer
    {
        public async Task<object> GetCheckoutDetails(string email, string couponCode)
        {
            return await _dataBaseLayer.GetCheckoutDetails(email, couponCode);
        }
        public async Task<object> CreateOrder(string email, CreateOrderModel model)
        {
            return await _dataBaseLayer.CreateOrder(email, model);
        }

        public async Task<object> VerifyPayment(RazorpayVerifyModel model)
        {
            return await _dataBaseLayer.VerifyPayment(model);
        }

        public async Task<object> GetUserOrders(string email)
        {
            return await _dataBaseLayer.GetUserOrders(email);
        }

        public async Task<object> CancelOrder(string email, Guid orderId)
        {
            return await _dataBaseLayer.CancelOrder(email, orderId);
        }

        public async Task<object> RequestExchange(string email, ExchangeRequestModel model)
        {
            return await _dataBaseLayer.RequestExchange(email, model);
        }

        public async Task<object> GetExchangeRequests(string email, bool isAdmin)
        {
            return await _dataBaseLayer.GetExchangeRequests(email, isAdmin);
        }

        public async Task<object> UpdateExchangeStatus(UpdateExchangeStatusModel model)
        {
            return await _dataBaseLayer.UpdateExchangeStatus(model);
        }

        public async Task<object> SchedulePickup(PickupRequestModel model)
        {
            return await _dataBaseLayer.SchedulePickup(model);
        }

        public async Task<object> CompleteExchange(Guid exchangeId)
        {
            return await _dataBaseLayer.CompleteExchange(exchangeId);
        }

        public async Task<object> UpdatePickupStatus(Guid exchangeId, string status)
        {
            return await _dataBaseLayer.UpdatePickupStatus(exchangeId, status);
        }

        public async Task<object> GetMyOrders(string email)
        {
            return await _dataBaseLayer.GetMyOrders(email);
        }

        public async Task<object> UpdateOrderStatus(UpdateOrderStatusModel model)
        {
            return await _dataBaseLayer.UpdateOrderStatus(model);
        }

        public async Task<object> TrackOrder(Guid orderId)
        {
            return await _dataBaseLayer.TrackOrder(orderId);
        }

        public async Task<object> GetOrderDetails(string email, Guid orderId)
        {
            return await _dataBaseLayer.GetOrderDetails(email, orderId);
        }
    }
}
