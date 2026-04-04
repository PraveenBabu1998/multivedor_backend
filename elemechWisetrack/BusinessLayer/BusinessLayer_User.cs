using elemechWisetrack.Models;
using Microsoft.AspNetCore.Mvc;

namespace elemechWisetrack.BusinessLayer
{
    public interface iBusinessLayer_User
    {
        Task<object> AddWishListProduct(string productId, string email, string ipAddress);
        Task<object> GetWishListProduct(string email, string ipAddress);
        Task<object> DeleteWishListProduct(string productId, string email, string ipAddress);
        Task<object> CreateContact(ContactUsModel model);
        Task<object> GetAllContacts();
        Task<object> GetContactById(Guid id);
        Task<object> UpdateStatus(Guid id, string status);
        Task<object> DeleteContact(Guid id);
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

        public async Task<object> CreateContact(ContactUsModel model)
        {
            return await _dataBaseLayer.CreateContact(model);
        }
        public async Task<object> GetAllContacts()
        {
            return await _dataBaseLayer.GetAllContacts();
        }
        public async Task<object> GetContactById(Guid id)
        {
            return await _dataBaseLayer.GetContactById(id);
        }
        public async Task<object> UpdateStatus(Guid id, string status)
        {
            return await _dataBaseLayer.UpdateStatus(id, status);
        }
        public async Task<object> DeleteContact(Guid id)
        {
          return  await _dataBaseLayer.DeleteContact(id);
        }
    }
}
