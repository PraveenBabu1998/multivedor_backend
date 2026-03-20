using elemechWisetrack.BusinessLayer;
using elemechWisetrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace elemechWisetrack.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/brands")]

    public class BrandsController : ControllerBase
    {
        private readonly IBusinessLayer _businessLayer;
        public BrandsController(IBusinessLayer businessLayer)
        {
            _businessLayer = businessLayer;
        }

        [HttpPost("insert")]
        public async Task<IActionResult> AddBrands([FromForm] BrandInsertModel request)
        {
            string userEmail =
                User.FindFirst(ClaimTypes.Email)?.Value ??
                User.FindFirst("email")?.Value ??
                User.FindFirst("UserEmail")?.Value;

            var data = await _businessLayer.AddBrands(userEmail, request);

            return Ok(new
            {
                Success = true,
                Message = "Brand added successfully",
                data
            });
        }

        [HttpGet("list")]
        [AllowAnonymous]
        public async Task<IActionResult> GetBrands()
        {
            var result = await _businessLayer.GetBrands();
            return Ok(result);
        }

        [HttpGet("list/{id}")]
        public async Task<IActionResult> GetBrandsById(Guid id)
        {
            var brand = await _businessLayer.GetBrandById(id);

            return Ok(new
            {
                success = true,
                data = brand
            });
        }

        [HttpPut("update/{id}")]
        public async Task<IActionResult> UpdateBrandsById(Guid id, [FromForm] BrandInsertModel request)
        {
            string userEmail =
                User.FindFirst(ClaimTypes.Email)?.Value ??
                User.FindFirst("email")?.Value ??
                User.FindFirst("UserEmail")?.Value;

            var data = await _businessLayer.UpdateBrandsById(id, userEmail, request);

            return Ok(new
            {
                Success = true,
                Message = "Brand updated successfully",
                data
            });
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteBrandsById(Guid id)
        {
            if (id == Guid.Empty)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid Brand Id"
                });
            }

            try
            {
                var brand = await _businessLayer.DeleteBrandsById(id);

                if (brand == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Brand not found"
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = brand
                });
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

        [HttpPost("status/{id}")]
        public async Task<IActionResult> ToggleBrandsById(Guid id)
        {
            if (id == Guid.Empty)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid Brand Id"
                });
            }

            try
            {
                var brand = await _businessLayer.ToggleBrandsById(id);

                if (brand == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Brand not found"
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = brand
                });
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



    }
}
