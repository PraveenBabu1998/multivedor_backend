using elemechWisetrack.Models;
using Microsoft.EntityFrameworkCore;

namespace elemechWisetrack.BusinessLayer
{
    public interface IBusinessLayer_Address
    {
        Task<AppUser> GetUserByEmailAsync(string email);

        Task<Guid> AddAddressAsync(Guid userId, AddressRequest request);
         Task<List<AddressDetails>> GetAddressesAsync(Guid userId);
         Task<AddressDetails> GetAddressByIdAsync(Guid id, Guid userId);
         Task<bool> UpdateAddressAsync(Guid id, Guid userId, AddressRequest request);
         Task<bool> DeleteAddressAsync(Guid id, Guid userId);
        
    }

    public partial interface IBusinessLayer : IBusinessLayer_Address { }

    public partial class BusinessLayer
    {
        public async Task<AppUser> GetUserByEmailAsync(string email)
        {
            return await _dataBaseLayer.GetUserByEmailAsync(email);
        }

        public async Task<Guid> AddAddressAsync(Guid userId, AddressRequest request)
        {
            return await  _dataBaseLayer.AddAddressAsync(userId, request);
        }
        public async Task<List<AddressDetails>> GetAddressesAsync(Guid userId)
        {
            return await _dataBaseLayer.GetAddressesAsync(userId);
        }
        public async Task<AddressDetails> GetAddressByIdAsync(Guid id, Guid userId)
        {
            return await _dataBaseLayer.GetAddressByIdAsync(id, userId);
        }
        public async Task<bool> UpdateAddressAsync(Guid id, Guid userId, AddressRequest request)
        {
            return await _dataBaseLayer.UpdateAddressAsync(id, userId, request);
        }
        public async Task<bool> DeleteAddressAsync(Guid id, Guid userId)
        {
            return await _dataBaseLayer.DeleteAddressAsync(id, userId);
        }
    }
}
