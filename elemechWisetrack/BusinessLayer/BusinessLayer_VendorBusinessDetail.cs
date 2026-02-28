using elemechWisetrack.Models;
using Microsoft.AspNetCore.Mvc;

namespace elemechWisetrack.BusinessLayer
{
    public interface IBusinessLayer_VendorBusinessDetail
    {
        Task<object> AddVendorBusinessDetail(string userEmail, [FromBody] VendorBusinessDetails request);
        Task<object> GetVendorBusinessDetail();
        Task<object> GetVendorBusinessDatailByEmail(string userEmail);
        Task<object> UpdateVendorBusinessDetail(Guid busiDetailId,string userEmail, [FromBody] VendorBusinessDetails request);
        Task<object> DeleteVendorBusinessDetail(Guid busiDetailId);
    }

    public partial interface IBusinessLayer : IBusinessLayer_VendorBusinessDetail
    {
        
    }

    public partial class BusinessLayer
    {
        public async Task<object> AddVendorBusinessDetail(string userEmail, VendorBusinessDetails request)
        {
            try
            {
                // ✅ Check null request
                if (request == null)
                    return new BadRequestObjectResult("Request body is required.");

                // ✅ Check userEmail
                if (string.IsNullOrWhiteSpace(userEmail))
                    return new BadRequestObjectResult("User email is required.");

                // ✅ Required Business Fields
                if (string.IsNullOrWhiteSpace(request.BusinessName))
                    return new BadRequestObjectResult("Business Name is required.");

                if (string.IsNullOrWhiteSpace(request.AddressLine1))
                    return new BadRequestObjectResult("Address Line1 is required.");

                if (string.IsNullOrWhiteSpace(request.City))
                    return new BadRequestObjectResult("City is required.");

                if (string.IsNullOrWhiteSpace(request.State))
                    return new BadRequestObjectResult("State is required.");

                if (string.IsNullOrWhiteSpace(request.Pincode))
                    return new BadRequestObjectResult("Pincode is required.");

                // Optional: GST format basic validation
                if (!string.IsNullOrEmpty(request.GSTNumber) && request.GSTNumber.Length != 15)
                    return new BadRequestObjectResult("Invalid GST Number.");

                // Optional: Phone validation
                if (!string.IsNullOrEmpty(request.BusinessPhone) && request.BusinessPhone.Length < 10)
                    return new BadRequestObjectResult("Invalid Business Phone number.");

                // ✅ If everything valid → Call Database Layer
                return await _dataBaseLayer.AddVendorBusinessDetail(userEmail, request);
            }
            catch (Exception ex)
            {
                return new ObjectResult(new
                {
                    Status = false,
                    Message = "Something went wrong",
                    Error = ex.Message
                })
                { StatusCode = 500 };
            }
        }

        public async Task<object> GetVendorBusinessDetail()
        {
            return await _dataBaseLayer.GetVendorBusinessDetail();
        }

        public async Task<object> GetVendorBusinessDatailByEmail(string userEmail)
        {
            return await _dataBaseLayer.GetVendorBusinessDatailByEmail(userEmail);
        }

        public async Task<object> UpdateVendorBusinessDetail(Guid busiDetailId,string userEmail, [FromBody] VendorBusinessDetails request)
        {
            try
            {
                // ✅ Check null request
                if (request == null)
                    return new BadRequestObjectResult("Request body is required.");

                // ✅ Check userEmail
                if (string.IsNullOrWhiteSpace(userEmail))
                    return new BadRequestObjectResult("User email is required.");

                // ✅ Required Business Fields
                if (string.IsNullOrWhiteSpace(request.BusinessName))
                    return new BadRequestObjectResult("Business Name is required.");

                if (string.IsNullOrWhiteSpace(request.AddressLine1))
                    return new BadRequestObjectResult("Address Line1 is required.");

                if (string.IsNullOrWhiteSpace(request.City))
                    return new BadRequestObjectResult("City is required.");

                if (string.IsNullOrWhiteSpace(request.State))
                    return new BadRequestObjectResult("State is required.");

                if (string.IsNullOrWhiteSpace(request.Pincode))
                    return new BadRequestObjectResult("Pincode is required.");

                // Optional: GST format basic validation
                if (!string.IsNullOrEmpty(request.GSTNumber) && request.GSTNumber.Length != 15)
                    return new BadRequestObjectResult("Invalid GST Number.");

                // Optional: Phone validation
                if (!string.IsNullOrEmpty(request.BusinessPhone) && request.BusinessPhone.Length < 10)
                    return new BadRequestObjectResult("Invalid Business Phone number.");

                // ✅ If everything valid → Call Database Layer
                return await _dataBaseLayer.UpdateVendorBusinessDetail(busiDetailId,userEmail, request);
            }
            catch (Exception ex)
            {
                return new ObjectResult(new
                {
                    Status = false,
                    Message = "Something went wrong",
                    Error = ex.Message
                })
                { StatusCode = 500 };
            }
        }

        public async Task<object> DeleteVendorBusinessDetail(Guid busiDetailId)
        {
            return await _dataBaseLayer.DeleteVendorBusinessDetail(busiDetailId);
        }

    }
}
