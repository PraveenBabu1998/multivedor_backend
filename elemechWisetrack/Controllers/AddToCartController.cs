using elemechWisetrack.BusinessLayer;
using elemechWisetrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace elemechWisetrack.Controllers
{
    [ApiController]
    [Route("api/cart")]
    [Authorize]
    public class AddToCartController : ControllerBase
    {
        private readonly IBusinessLayer _businessLayer;

        public AddToCartController(IBusinessLayer businessLayer)
        {
            _businessLayer = businessLayer;
        }

        // ✅ Add to Cart
        [HttpPost("add")]
        public async Task<IActionResult> AddToCart(AddToCartModel model)
        {
            string userEmail = User.FindFirst(ClaimTypes.Email)?.Value ??
                                   User.FindFirst("email")?.Value ??
                                   User.FindFirst("UserName")?.Value;
            var result = await _businessLayer.AddToCart(userEmail, model);
            return Ok(result);
        }

        // ✅ Get Cart
        [HttpGet("list")]
        public async Task<IActionResult> GetCart()
        {
            string userEmail = User.FindFirst(ClaimTypes.Email)?.Value ??
                   User.FindFirst("email")?.Value ??
                   User.FindFirst("UserName")?.Value;
            var data = await _businessLayer.GetCart(userEmail);
            return Ok(data);
        }

        // ✅ Update Quantity
        [HttpPut("update")]
        public async Task<IActionResult> UpdateCart(UpdateCartModel model)
        {
            string userEmail = User.FindFirst(ClaimTypes.Email)?.Value ??
                   User.FindFirst("email")?.Value ??
                   User.FindFirst("UserName")?.Value;
            var result = await _businessLayer.UpdateCart(userEmail, model);
            return Ok(result);
        }

        // ✅ Remove Item
        [HttpDelete("remove")]
        public async Task<IActionResult> RemoveItem(RemoveCartModel model)
        {
            string userEmail = User.FindFirst(ClaimTypes.Email)?.Value ??
                   User.FindFirst("email")?.Value ??
                   User.FindFirst("UserName")?.Value;
            var result = await _businessLayer.RemoveItem(userEmail, model.ProductId);
            return Ok(result);
        }

        // ✅ Clear Cart
        [HttpDelete("clear")]
        public async Task<IActionResult> ClearCart()
        {
            string userEmail = User.FindFirst(ClaimTypes.Email)?.Value ??
                   User.FindFirst("email")?.Value ??
                   User.FindFirst("UserName")?.Value;
            var result = await _businessLayer.ClearCart(userEmail);
            return Ok(result);
        }
    }
}