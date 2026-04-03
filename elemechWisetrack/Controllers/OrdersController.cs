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

        [HttpPost("checkout")]
        public async Task<IActionResult> GetCheckoutDetails([FromBody] CheckoutRequest model)
        {
            string email = User.FindFirst(ClaimTypes.Email)?.Value ??
                           User.FindFirst("email")?.Value ??
                           User.FindFirst("UserName")?.Value;

            if (string.IsNullOrEmpty(email))
                return Unauthorized();

            var result = await _businessLayer.GetCheckoutDetails(email, model.CouponCode);

            return Ok(result);
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create(CreateOrderModel model)
        {
            string email = User.FindFirst(ClaimTypes.Email)?.Value ??
                           User.FindFirst("email")?.Value ??
                           User.FindFirst("UserName")?.Value;

            if (string.IsNullOrEmpty(email))
                return Unauthorized();

            var result = await _businessLayer.CreateOrder(email, model);
            return Ok(result);
        }

        [HttpPost("verify")]
        public async Task<IActionResult> Verify(RazorpayVerifyModel model)
        {
            var result = await _businessLayer.VerifyPayment(model);
            return Ok(result);
        }

        [HttpGet("orders-history")]
        public async Task<IActionResult> GetOrders()
        {
            string email = User.FindFirst(ClaimTypes.Email)?.Value ??
                                   User.FindFirst("email")?.Value ??
                                   User.FindFirst("UserName")?.Value;

            var data = await _businessLayer.GetUserOrders(email);

            return Ok(data);
        }

        [HttpGet("my-orders")]
        public async Task<IActionResult> GetMyOrders()
        {
            // ✅ Get email from JWT token
            string email = User.FindFirst(ClaimTypes.Email)?.Value ??
                                   User.FindFirst("email")?.Value ??
                                   User.FindFirst("UserName")?.Value;

            if (string.IsNullOrEmpty(email))
            {
                return Unauthorized(new { success = false, message = "Invalid token" });
            }

            var result = await _businessLayer.GetMyOrders(email);

            return Ok(result);
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

        [HttpPost("update-order-status")]
        public async Task<IActionResult> UpdateOrderStatus(UpdateOrderStatusModel model)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value ??
                                   User.FindFirst("email")?.Value ??
                                   User.FindFirst("UserName")?.Value; // or from JWT claim

            model.UpdatedByEmail = email;

            var result = await _businessLayer.UpdateOrderStatus(model);
            return Ok(result);
        }

        [HttpGet("track-order/{orderId}")]
        public async Task<IActionResult> TrackOrder(Guid orderId)
        {
            var result = await _businessLayer.TrackOrder(orderId);
            return Ok(result);
        }

        [HttpGet("order-details/{orderId}")]
        public async Task<IActionResult> GetOrderDetails(Guid orderId)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value ??
                                   User.FindFirst("email")?.Value ??
                                   User.FindFirst("UserName")?.Value; // or claim

            var result = await _businessLayer.GetOrderDetails(email, orderId);
            return Ok(result);
        }
    }
}
