using elemechWisetrack.BusinessLayer;
using elemechWisetrack.Models;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Asn1.Crmf;
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

        [HttpPost("list")]
        public async Task<IActionResult> GetCart([FromBody] CartRequest model)
        {
            var (email, ip) = GetUserOrGuest();
            var result = await _businessLayer.GetCart(email, ip, model.CouponCode);
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

        [HttpPost("update-quantity")]
        public async Task<IActionResult> UpdateCartQuantity(UpdateCartQuantityModel model)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            var result = await _businessLayer.UpdateCartQuantity(email, model);

            return Ok(result);
        }
    }
}