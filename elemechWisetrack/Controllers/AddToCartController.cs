using elemechWisetrack.BusinessLayer;
using elemechWisetrack.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace elemechWisetrack.Controllers
{
    [ApiController]
    [Route("api/cart")]
    public class AddToCartController : ControllerBase
    {
        private readonly IBusinessLayer _businessLayer;

        public AddToCartController(IBusinessLayer businessLayer)
        {
            _businessLayer = businessLayer;
        }

        private (string email, string ip) GetUserOrGuest()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value ??
                        User.FindFirst("email")?.Value;

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            return (email, ip);
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddToCart(AddToCartModel model)
        {
            var (email, ip) = GetUserOrGuest();
            var result = await _businessLayer.AddToCart(email, ip, model);
            return Ok(result);
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetCart()
        {
            var (email, ip) = GetUserOrGuest();
            var result = await _businessLayer.GetCart(email, ip);
            return Ok(result);
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateCart(UpdateCartModel model)
        {
            var (email, ip) = GetUserOrGuest();
            var result = await _businessLayer.UpdateCart(email, ip, model);
            return Ok(result);
        }

        [HttpDelete("remove")]
        public async Task<IActionResult> RemoveItem(RemoveCartModel model)
        {
            var (email, ip) = GetUserOrGuest();
            var result = await _businessLayer.RemoveItem(email, ip, model.ProductId);
            return Ok(result);
        }

        [HttpDelete("clear")]
        public async Task<IActionResult> ClearCart()
        {
            var (email, ip) = GetUserOrGuest();
            var result = await _businessLayer.ClearCart(email, ip);
            return Ok(result);
        }
    }
}