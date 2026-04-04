using elemechWisetrack.BusinessLayer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace elemechWisetrack.Controllers
{
    [ApiController]
    [Route("api/user")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IBusinessLayer _businessLayer;
        public UserController(IBusinessLayer businessLayer)
        {
            _businessLayer = businessLayer;
        }


        private string GetIpAddress()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString();
        }

        [HttpPost("add/{productId}")]
        [AllowAnonymous]
        public async Task<IActionResult> AddWishListProduct(string productId)
        {
            if (string.IsNullOrEmpty(productId))
                return BadRequest("Product is required");

            string email = User.FindFirst(ClaimTypes.Email)?.Value;
            string ipAddress = GetIpAddress();

            return Ok(await _businessLayer.AddWishListProduct(productId, email, ipAddress));
        }

        // ✅ GET WISHLIST
        [HttpGet("list")]
        [AllowAnonymous]
        public async Task<IActionResult> GetWishListProduct()
        {
            string email = User.FindFirst(ClaimTypes.Email)?.Value;
            string ipAddress = GetIpAddress();

            return Ok(await _businessLayer.GetWishListProduct(email, ipAddress));
        }

        // ✅ DELETE WISHLIST
        [HttpDelete("delete/{productId}")]
        [AllowAnonymous]
        public async Task<IActionResult> DeleteWishListProduct(string productId)
        {
            if (string.IsNullOrEmpty(productId))
                return BadRequest("Product is required");

            string email = User.FindFirst(ClaimTypes.Email)?.Value;
            string ipAddress = GetIpAddress();

            return Ok(await _businessLayer.DeleteWishListProduct(productId, email, ipAddress));
        }
    }
}


