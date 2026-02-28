using elemechWisetrack.BusinessLayer;
using elemechWisetrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace elemechWisetrack.Controllers
{
    [ApiController]
    [Authorize (Roles ="ADMIN, SUPERADMIN")]
    [Route("api/products")]
    public class ProductsController : ControllerBase
    {
        private readonly IBusinessLayer _businessLayer;
        public ProductsController(IBusinessLayer businessLayer)
        {
            _businessLayer = businessLayer;
        }

        [Route("insert")]
        [HttpPost]
        public async Task<IActionResult> AddSingleProduct([FromBody] ProductInsertModel request)
        {

            try
            {
                string userEmail = User.FindFirst(ClaimTypes.Email)?.Value ??
                    User.FindFirst("email")?.Value ??
                    User.FindFirst("UserName").Value;

                if (string.IsNullOrEmpty(userEmail))
                {
                    BadRequest("User Not Found");
                }

                var result = _businessLayer.AddSingleProduct(userEmail, request);

                return Ok(new
                {
                    Success = true,
                    data = result,
                });
            }
            catch(Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = ex.Message,

                });
            }
            
        }

        [Route("list")]
        [HttpGet]
        public async Task<IActionResult> GetAllProducts(int? page, int? pageSize)
        {
            var data = await _businessLayer.GetAllProducts(page, pageSize);
            return Ok(new {Success = true, Message = "Product list successfully",data = data});
        }

        [Route("update/{productId}")]
        [HttpPut]
        public async Task<IActionResult> UpdateProduct(
    Guid productId,
    ProductInsertModel request)
        {

            string userEmail = User.FindFirst(ClaimTypes.Email)?.Value ??
                User.FindFirst("UserEmail")?.Value ??
                User.FindFirst("email")?.Value;
            var data = await _businessLayer.UpdateProduct(productId, userEmail, request);
            return Ok(new
            {
                Success = true,
                Message = "Product update successfully",
                data = data
            });
        }
    }
}
