using elemechWisetrack.BusinessLayer;
using elemechWisetrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NuGet.Protocol.Plugins;
using System.Security.Claims;

namespace elemechWisetrack.Controllers
{
    [ApiController]
    [Route("api/bank")]
    [Authorize]
    public class VendorBankDetailController : ControllerBase
    {
        private readonly IBusinessLayer _businessLayer;
        public VendorBankDetailController(IBusinessLayer businessLayer)
        {
            _businessLayer = businessLayer;
        }


        [Route("add/{userId}")]
        [HttpPost]
        public async Task<IActionResult> AddBank(Guid userId, [FromBody] VandorBankDetail request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.BankName) ||
                    string.IsNullOrWhiteSpace(request.AccountHolderName) ||
                    string.IsNullOrWhiteSpace(request.AccountNumber) ||
                    string.IsNullOrWhiteSpace(request.IFSCCode))
                {
                    return BadRequest("All required fields must be provided!");
                }

                var data = await _businessLayer.AddBankDetail(userId, request);

                return Ok(new
                {
                    data = data,
                    message = "Added successfully!"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }
        }

        [Route("get/{userId}")]
        [HttpGet]
        public async Task<IActionResult> GetBankDetailByUserId(Guid userId)
        {
            if (userId.Equals(Guid.Empty))
            {
                return BadRequest("UserId is required.");
            }

            try
            {
                var data = await _businessLayer.GetBankDetailByUserId(userId);

                if (data == null)
                {
                    return NotFound(new
                    {
                        Success = false,
                        Message = "Bank details not found"
                    });
                }

                return Ok(new
                {
                    Success = true,
                    Data = data
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }

        }


        [Route("get")]
        [HttpGet]
        public async Task<IActionResult> GetBankDetail()
        {

            try
            {
                var data = await _businessLayer.GetVendorBankDetail();

                if (data == null)
                {
                    return NotFound(new
                    {
                        Success = false,
                        Message = "Bank details not found"
                    });
                }

                return Ok(new
                {
                    Success = true,
                    Data = data
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }

        }

        [HttpPut("update-bank/{bankDetailId}")]
        public async Task<IActionResult> UpdateBank(int bankDetailId, [FromBody] VandorBankDetail request)
        {
            try
            {

                string userEmail =
                   User.FindFirst(ClaimTypes.Email)?.Value ??
                   User.FindFirst("email")?.Value ??
                   User.FindFirst("UserEmail")?.Value;



                var result = await _businessLayer.UpdateBankDetail(userEmail, bankDetailId, request);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        [Route("delete/{bankId}")]
        [HttpDelete]
        public async Task<IActionResult>DeleteVendorBankDetail(int bankId)
        {
            if (bankId !=0)
            {
                BadRequest("Bank detail not found!");
            }

            try
            {
                var bankdata = await _businessLayer.DeleteVendorBankDetail(bankId);
                return Ok(new
                {
                    success = true,
                    data = bankdata,
                    message = " Bank Detail delete successfully"
                });
            }
            catch(Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

    }
}
