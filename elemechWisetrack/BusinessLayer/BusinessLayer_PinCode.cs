using elemechWisetrack.Models;

namespace elemechWisetrack.BusinessLayer
{
    public interface IBusinessLayer_PinCode
    {
        Task<object> AddPinCode(string userEmail, AddPincodeRequest model);
        Task<object> GetAllPinCodes();
        Task<object> CheckPincode(string pincode);
        Task<object> UpdatePinCode(Guid id, string userEmail, AddPincodeRequest model);
        Task<object> DeletePinCode(Guid id);
    }

    public partial interface IBusinessLayer : IBusinessLayer_PinCode { }

    public partial class BusinessLayer
    {
        public async Task<object> AddPinCode(string userEmail, AddPincodeRequest model)
        {
            if (string.IsNullOrEmpty(userEmail))
                return new { Success = false, Message = "Unauthorized" };

            return await _dataBaseLayer.AddPinCode(userEmail, model);
        }

        public async Task<object> GetAllPinCodes()
        {
            return await _dataBaseLayer.GetAllPinCodes();
        }

        public async Task<object> CheckPincode(string pincode)
        {
            return await _dataBaseLayer.CheckPincode(pincode);
        }

        public async Task<object> UpdatePinCode(Guid id, string userEmail, AddPincodeRequest model)
        {
            return await _dataBaseLayer.UpdatePinCode(id, userEmail, model);
        }

        public async Task<object> DeletePinCode(Guid id)
        {
            return await _dataBaseLayer.DeletePinCode(id);
        }
    }
}