using elemechWisetrack.BusinessLayer;
using elemechWisetrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace elemechWisetrack.Controllers
{
    [ApiController]
    [Route("api/orders")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly IBusinessLayer _businessLayer;
        public OrdersController(IBusinessLayer businessLayer)
        {
            this._businessLayer = businessLayer;
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create(CreateOrderModel model)
        {
            string userEmail = User.FindFirst(ClaimTypes.Email)?.Value ??
                                   User.FindFirst("email")?.Value ??
                                   User.FindFirst("UserName")?.Value;

            var result = await _businessLayer.CreateOrder(userEmail, model);
            return Ok(result);
        }

        [HttpPost("verify")]
        public async Task<IActionResult> Verify(RazorpayVerifyModel model)
        {
            var result = await _businessLayer.VerifyPayment(model);
            return Ok(result);
        }

        [HttpGet("my-orders")]
        public async Task<IActionResult> GetOrders()
        {
            string email = User.FindFirst(ClaimTypes.Email)?.Value ??
                                   User.FindFirst("email")?.Value ??
                                   User.FindFirst("UserName")?.Value;

            var data = await _businessLayer.GetUserOrders(email);

            return Ok(data);
        }

        [HttpPost("cancel/{orderId}")]
        public async Task<IActionResult> CancelOrder(Guid orderId)
        {
            string email = User.FindFirst(ClaimTypes.Email)?.Value ??
                                   User.FindFirst("email")?.Value ??
                                   User.FindFirst("UserName")?.Value;

            if (string.IsNullOrEmpty(email))
                return Unauthorized();

            var result = await _businessLayer.CancelOrder(email, orderId);

            return Ok(result);
        }

        [HttpPost("exchange")]
        public async Task<IActionResult> RequestExchange(ExchangeRequestModel model)
        {
            string email = User.FindFirst(ClaimTypes.Email)?.Value ??
                                   User.FindFirst("email")?.Value ??
                                   User.FindFirst("UserName")?.Value;

            if (string.IsNullOrEmpty(email))
                return Unauthorized();

            var result = await _businessLayer.RequestExchange(email, model);

            return Ok(result);
        }

        [HttpGet("exchange-list")]
        public async Task<IActionResult> GetMyExchangeRequests()
        {
            string email = User.FindFirst(ClaimTypes.Email)?.Value ??
                                   User.FindFirst("email")?.Value ??
                                   User.FindFirst("UserName")?.Value;

            if (string.IsNullOrEmpty(email))
                return Unauthorized();

            var result = await _businessLayer.GetExchangeRequests(email, false);

            return Ok(result);
        }

        [HttpPost("admin/exchange/update-status")]
        public async Task<IActionResult> UpdateExchangeStatus(UpdateExchangeStatusModel model)
        {
            var result = await _businessLayer.UpdateExchangeStatus(model);
            return Ok(result);
        }

        [HttpPost("exchange/pickup")]
        public async Task<IActionResult> SchedulePickup(PickupRequestModel model)
        {
            var result = await _businessLayer.SchedulePickup(model);
            return Ok(result);
        }

        [HttpPost("exchange/complete/{exchangeId}")]
        public async Task<IActionResult> CompleteExchange(Guid exchangeId)
        {
            var result = await _businessLayer.CompleteExchange(exchangeId);
            return Ok(result);
        }

        [HttpPost("exchange/pickup-status")]
        public async Task<IActionResult> UpdatePickupStatus([FromBody] PickupStatusUpdateModel model)
        {
            var result = await _businessLayer.UpdatePickupStatus(model.ExchangeId, model.Status);
            return Ok(result);
        }
    }
}
