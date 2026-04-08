using elemechWisetrack.Models;
using Microsoft.AspNetCore.Mvc;

namespace elemechWisetrack.BusinessLayer
{
    public interface IBusinessLayer_VendorBankDetail
    {
        Task<object> AddBankDetail(string userId, [FromBody] VandorBankDetail request);
        Task<object?> GetBankDetailByUserId(string userId);
        Task<object?> GetVendorBankDetail();
        Task<object> UpdateBankDetail(string userEmail, int bankDetailId, VandorBankDetail request);
        Task<object> DeleteVendorBankDetail(int bankId);
    }

    public partial interface IBusinessLayer : IBusinessLayer_VendorBankDetail
    {

    }

    public partial class BusinessLayer
    {
        public async Task<object> AddBankDetail(string userId, [FromBody] VandorBankDetail request)
        {
            var data = await _dataBaseLayer.AddBankDetail(userId,request);
            return (data);
        }

        public async Task<object?> GetBankDetailByUserId(string userId)
        {
            return await _dataBaseLayer.GetBankDetailByUserId(userId);
        }

        public async Task<object?> GetVendorBankDetail()
        {
            return await _dataBaseLayer.GetVendorBankDetail();
        }

        public async Task<object> UpdateBankDetail(string userEmail, int bankDetailId, VandorBankDetail request)
        {
            return await _dataBaseLayer.UpdateBankDetail(userEmail, bankDetailId, request);
        }

        public async Task<object> DeleteVendorBankDetail(int bankId)
        {
            return await _dataBaseLayer.DeleteVendorBankDetail(bankId);
        }
    }
}
