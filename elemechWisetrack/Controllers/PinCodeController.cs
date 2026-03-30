using elemechWisetrack.BusinessLayer;
using elemechWisetrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace elemechWisetrack.Controllers
{
    [ApiController]
    [Route("api/pincode")]
    [Authorize]
    public class PinCodeController : ControllerBase
    {
        private readonly IBusinessLayer _businessLayer;

        public PinCodeController(IBusinessLayer businessLayer)
        {
            _businessLayer = businessLayer;
        }

        private string GetUserEmail()
        {
            return User.FindFirst(ClaimTypes.Email)?.Value ??
                   User.FindFirst("email")?.Value ??
                   User.FindFirst("UserEmail")?.Value;
        }

        // ✅ ADD
        [HttpPost("add")]
        public async Task<IActionResult> AddPinCode(AddPincodeRequest model)
        {
            var result = await _businessLayer.AddPinCode(GetUserEmail(), model);
            return Ok(result);
        }

        // ✅ LIST
        [HttpGet("list")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _businessLayer.GetAllPinCodes();
            return Ok(result);
        }

        // ✅ CHECK (PUBLIC)
        [HttpGet("check/{pincode}")]
        [AllowAnonymous]
        public async Task<IActionResult> Check(string pincode)
        {
            var result = await _businessLayer.CheckPincode(pincode);
            return Ok(result);
        }

        // ✅ UPDATE
        [HttpPut("update/{id}")]
        public async Task<IActionResult> Update(Guid id, AddPincodeRequest model)
        {
            var result = await _businessLayer.UpdatePinCode(id, GetUserEmail(), model);
            return Ok(result);
        }

        // ✅ DELETE
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _businessLayer.DeletePinCode(id);
            return Ok(result);
        }
    }
}