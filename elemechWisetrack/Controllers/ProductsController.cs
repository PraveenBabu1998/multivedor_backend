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
        public async Task<IActionResult> AddSingleProduct([FromForm] ProductInsertModel request)
        {
            try
            {
                string userEmail = User.FindFirst(ClaimTypes.Email)?.Value ??
                                   User.FindFirst("email")?.Value ??
                                   User.FindFirst("UserName")?.Value;

                if (string.IsNullOrEmpty(userEmail))
                {
                    return BadRequest("User Not Found");
                }

                var result = await _businessLayer.AddSingleProduct(userEmail, request);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [Route("list")]
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllProducts(
    [FromQuery] int? page,
    [FromQuery] int? pageSize,

    [FromQuery] Guid[]? categoryIds,
    [FromQuery] Guid? subCategoryId,
    [FromQuery] Guid[]? brandIds,

    [FromQuery] string[]? colors,
    [FromQuery] string[]? sizes,

    [FromQuery] decimal? minPrice,
    [FromQuery] decimal? maxPrice,

    [FromQuery] string? search
)
        {
            var data = await _businessLayer.GetAllProducts(
                page, pageSize,
                categoryIds, subCategoryId, brandIds,
                colors, sizes,
                minPrice, maxPrice,
                search
            );

            return Ok(new
            {
                success = true,
                message = "Product list successfully",
                data
            });
        }

        [Route("get/{productId}")]
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetProductById(Guid productId)
        {
            var data = await _businessLayer.GetProductById(productId);
            return Ok(new { Success = true, Message = "Product get successfully", data = data });
        }

        [Route("list-by-admin")]
        [HttpGet]
        public async Task<IActionResult> GetAllProductsOfAdmin(int? page, int? pageSize)
        {

            string userEmail = User.FindFirst(ClaimTypes.Email)?.Value ??
                User.FindFirst("UserEmail")?.Value ??
                User.FindFirst("email")?.Value;
            var data = await _businessLayer.GetAllProductsOfAdmin(userEmail, page, pageSize);
            return Ok(new { Success = true, Message = "Product list successfully", data = data });
        }

        [Route("update/{productId}")]
        [HttpPut]
        public async Task<IActionResult> UpdateProduct(
    Guid productId,
    [FromForm] ProductInsertModel request)
        {
            string userEmail = User.FindFirst(ClaimTypes.Email)?.Value ??
                               User.FindFirst("UserEmail")?.Value ??
                               User.FindFirst("email")?.Value;

            var data = await _businessLayer.UpdateProduct(productId, userEmail, request);

            return Ok(new
            {
                Success = true,
                Message = "Product updated successfully",
                data = data
            });
        }

        [HttpDelete("soft-delete-product/{productId}")]
        public async Task<IActionResult> DeleteProduct(Guid productId)
        {
            var result = await _businessLayer.DeleteProduct(productId);
            return Ok(result);
        }

        [HttpPut("restore-product/{productId}")]
        public async Task<IActionResult> RestoreProduct(Guid productId)
        {
            var result = await _businessLayer.RestoreProduct(productId);
            return Ok(result);
        }

        [HttpDelete("permanent-delete-product/{productId}")]
        public async Task<IActionResult> PermanentDeleteProduct(Guid productId)
        {
            var result = await _businessLayer.PermanentDeleteProduct(productId);
            return Ok(result);
        }

        [HttpPost("upload-excel")]
        public async Task<IActionResult> UploadProductsExcel(IFormFile file)
        {
            string userEmail = User.FindFirst(ClaimTypes.Email)?.Value??
                User.FindFirst("email")?.Value??
                User.FindFirst("userName")?.Value;
            if (file == null || file.Length == 0)
                return BadRequest("File is required");

            // Get file extension
            var extension = Path.GetExtension(file.FileName).ToLower();

            // Allowed Excel extensions
            string[] allowedExtensions = { ".xls", ".xlsx", ".csv" };

            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest("Invalid file format. Only .xls and .xlsx files are allowed.");
            }

            var result =await _businessLayer.UploadProductsExcel(file,userEmail);

            return Ok(new {Success = false,Message = "Excel file is valid", result });
        }

        [HttpPut("toggle-sales-status/{productId}")]
        public async Task<IActionResult> ToggleSalesStatus(Guid productId)
        {
            var result = await _businessLayer.ToggleSalesStatus(productId);

            return Ok(new
            {
                success = true,
                message = "Sales status updated successfully",
                data = result
            });
        }

        [HttpGet("sales-products")]
        [AllowAnonymous]
        public async Task<IActionResult> GetSalesProducts(
    [FromQuery] int? page,
    [FromQuery] int? pageSize,

    [FromQuery] Guid[]? categoryIds,
    [FromQuery] Guid? subCategoryId,
    [FromQuery] Guid[]? brandIds,

    [FromQuery] string[]? colors,
    [FromQuery] string[]? sizes,

    [FromQuery] decimal? minPrice,
    [FromQuery] decimal? maxPrice,

    [FromQuery] string? search
)
        {
            var data = await _businessLayer.GetSalesProducts(
                page, pageSize,
                categoryIds, subCategoryId, brandIds,
                colors, sizes,
                minPrice, maxPrice,
                search
            );

            return Ok(new
            {
                success = true,
                message = "Sales product list",
                data
            });
        }

    }
}
