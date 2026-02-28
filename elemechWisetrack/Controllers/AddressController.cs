using elemechWisetrack.BusinessLayer;
using elemechWisetrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace elemechWisetrack.Controllers
{
        [ApiController]
        [Route("api/address")]
        [Authorize]
    public class AddressController : ControllerBase
    {
        private readonly IBusinessLayer _businessLayer;

        public AddressController(IBusinessLayer businessLayer)
        {
            _businessLayer = businessLayer;
        }

        private async Task<Guid> GetUserIdAsync()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(email))
                throw new UnauthorizedAccessException("Email claim not found in token.");

            var user = await _businessLayer.GetUserByEmailAsync(email);

            if (user == null)
                throw new UnauthorizedAccessException("User not found.");

            return user.Id;
        }

        [HttpPost("add")]
        public async Task<IActionResult> Add([FromBody] AddressRequest request)
        {
            var userId = await GetUserIdAsync();
            var id = await _businessLayer.AddAddressAsync(userId, request);

            return Ok(new { message = "Address added successfully", address_id = id });
        }

        [HttpGet("list")]
        public async Task<IActionResult> List()
        {
            var userId = await GetUserIdAsync();
            return Ok(await _businessLayer.GetAddressesAsync(userId));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var userId = await GetUserIdAsync();
            var address = await _businessLayer.GetAddressByIdAsync(id, userId);

            return address == null ? NotFound() : Ok(address);
        }

        [HttpPut("update/{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] AddressRequest request)
        {
            var userId = await GetUserIdAsync();
            var updated = await _businessLayer.UpdateAddressAsync(id, userId, request);

            return updated ? Ok("Address updated") : NotFound();
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = await GetUserIdAsync();
            var deleted = await _businessLayer.DeleteAddressAsync(id, userId);

            return deleted ? Ok("Address deleted") : NotFound();
        }
    }
}
