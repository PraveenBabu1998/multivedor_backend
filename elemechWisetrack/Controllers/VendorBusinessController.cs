using elemechWisetrack.BusinessLayer;
using elemechWisetrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace elemechWisetrack.Controllers
{
    [ApiController]
    [Route("api/vendor/business")]
    [Authorize (Roles="ADMIN,SUPERADMIN")]

    public class VendorBusinessController : ControllerBase
    {
        private readonly IBusinessLayer _businessLayer;
        public VendorBusinessController(IBusinessLayer businessLayer)
        {
            _businessLayer = businessLayer;
        }

        [Route("insert")]
        [HttpPost]
        public async Task<IActionResult> AddVendorBusinessDetail([FromBody] VendorBusinessDetails request) 
        {
            if (request == null) 
            {
                BadRequest(" Some fields are required");
            }


            try
            {
                string userEmail =
                   User.FindFirst(ClaimTypes.Email)?.Value ??
                   User.FindFirst("email")?.Value ??
                   User.FindFirst("UserEmail")?.Value;

                var data = await _businessLayer.AddVendorBusinessDetail(userEmail, request);
                return Ok(new
                {
                    Suceess = true,
                    data = data,
                    Message = "Add BusinessDetail Successfully"
                });
            }
            catch (Exception ex) 
            { 
                return StatusCode(500, ex.Message);
            }
        }

        [Route("list")]
        [HttpGet]
        public async Task<IActionResult> GetVendorBusinessDetail()
        {
            try
            {
                var data = await _businessLayer.GetVendorBusinessDetail();
                return Ok(new { 
                    success = true,
                    data = data,
                    Message = "Vendor detail list successfully!"
                });
            }
            catch(Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [Route("get")]
        [HttpGet]
        public async Task<IActionResult> GetVendorBusinessDatailByEmail()
        {
            string userEmail =
                   User.FindFirst(ClaimTypes.Email)?.Value ??
                   User.FindFirst("email")?.Value ??
                   User.FindFirst("UserEmail")?.Value;

            if (string.IsNullOrEmpty(userEmail))
            {
                return BadRequest("User Not found!");
            }

            try
            {
                var data = await _businessLayer.GetVendorBusinessDatailByEmail(userEmail);
                return Ok(new
                {
                    Success = true,
                    data = data,
                    Message = "Get data successfully!"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [Route("update/{busiDetailId}")]
        [HttpPut]
        public async Task<IActionResult> UpdateVendorBusinessDetail(Guid busiDetailId, [FromBody] VendorBusinessDetails request)
        {
            if (request == null)
            {
                BadRequest(" Some fields are required");
            }


            try
            {
                string userEmail =
                   User.FindFirst(ClaimTypes.Email)?.Value ??
                   User.FindFirst("email")?.Value ??
                   User.FindFirst("UserEmail")?.Value;

                var data = await _businessLayer.UpdateVendorBusinessDetail(busiDetailId, userEmail, request);
                return Ok(new
                {
                    Suceess = true,
                    data = data,
                    Message = "Add BusinessDetail Successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Route("delete/{busiDetailId}")]
        [HttpDelete]
        public async Task<IActionResult> DeleteVendorBusinessDetail(Guid busiDetailId)
        {
            try
            {
                var data = await _businessLayer.DeleteVendorBusinessDetail(busiDetailId);
                return Ok(new { Success = true, data = data, Message = "Business detail delete successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }


}
