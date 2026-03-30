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

        [HttpPost("add/{productId}")]
        public async Task<IActionResult> AddWishListProduct(string productId)
        {
            if (string.IsNullOrEmpty(productId))
                return BadRequest("Product is required");

            string email = User.FindFirst(ClaimTypes.Email)?.Value;

            return Ok(await _businessLayer.AddWishListProduct(productId, email));
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetWishListProduct()
        {
            string email = User.FindFirst(ClaimTypes.Email)?.Value;

            return Ok(await _businessLayer.GetWishListProduct(email));
        }

        [HttpDelete("delete/{productId}")]
        public async Task<IActionResult> DeleteWishListProduct(string productId)
        {
            if (string.IsNullOrEmpty(productId))
                return BadRequest("Product is required");

            string email = User.FindFirst(ClaimTypes.Email)?.Value;

            return Ok(await _businessLayer.DeleteWishListProduct(productId, email));
        }
    }
}
