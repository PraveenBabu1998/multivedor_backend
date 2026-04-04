using elemechWisetrack.BusinessLayer;
using elemechWisetrack.Models;
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

        // CREATE
        [HttpPost("contact")]
        public async Task<IActionResult> Create(ContactUsModel model)
        {
            var result = await _businessLayer.CreateContact(model);
            return Ok(result);
        }

        // GET ALL
        [HttpGet("contact")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _businessLayer.GetAllContacts();
            return Ok(result);
        }

        // GET BY ID
        [HttpGet("contact/{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _businessLayer.GetContactById(id);
            return Ok(result);
        }

        // UPDATE STATUS
        [HttpPut("contact/status/{id}")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromQuery] string status)
        {
            var result = await _businessLayer.UpdateStatus(id, status);
            return Ok(result);
        }

        // DELETE
        [HttpDelete("contact/delete/{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _businessLayer.DeleteContact(id);
            return Ok(result);
        }
    }
}


