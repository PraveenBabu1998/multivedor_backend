using elemechWisetrack.BusinessLayer;
using elemechWisetrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace elemechWisetrack.Controllers
{
    [ApiController]
    [Route("api/size")]
    [Authorize]
    public class SizeController : ControllerBase
    {
        private readonly IBusinessLayer _businessLayer;
        public SizeController(IBusinessLayer businessLayer)
        {
            _businessLayer = businessLayer;
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddSize(ProductSizes request)
        {
            string userEmail = User.FindFirst(ClaimTypes.Email)?.Value ??
                                   User.FindFirst("UserName")?.Value ??
                                   User.FindFirst("email")?.Value;

            var result = await _businessLayer.AddSize(userEmail, request);
            return Ok(result);
        }

        [HttpGet("get")]
        [AllowAnonymous]
        public async Task<IActionResult> GetSizes()
        {
            var result = await _businessLayer.GetSizes();
            return Ok(result);
        }

        [HttpPut("update/{id}")]
        public async Task<IActionResult> UpdateSize(Guid id, ProductSizes request)
        {
            string userEmail = User.FindFirst(ClaimTypes.Email)?.Value ??
                               User.FindFirst("UserName")?.Value ??
                               User.FindFirst("email")?.Value;

            var result = await _businessLayer.UpdateSize( id, request);

            return Ok(result);
        }

        [HttpPatch("toggle/{id}")]
        public async Task<IActionResult> ToggleSize(Guid id)
        {
            var result = await _businessLayer.ToggleSizeStatus(id);
            return Ok(result);
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteSize(Guid id)
        {
            var result = await _businessLayer.SoftDeleteSize(id);
            return Ok(result);
        }


        [HttpDelete("delete-size-permanent/{id}")]
        public async Task<IActionResult> DeleteSizePermanent(Guid id)
        {
            var result = await _businessLayer.DeleteSize(id);
            return Ok(result);
        }

    }
}
