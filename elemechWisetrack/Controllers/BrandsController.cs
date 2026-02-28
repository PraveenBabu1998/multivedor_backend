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

        [Route("insert")]
        [HttpPost]
        public async Task <IActionResult> AddBrands([FromBody] BrandInsertModel request)
        
        {
            string userEmail =
                User.FindFirst(ClaimTypes.Email)?.Value ??
                User.FindFirst("email")?.Value ??
                User.FindFirst("UserEmail")?.Value;

            try
            {
                var data = await _businessLayer.AddBrands(userEmail,request);
                return Ok(new
                {
                    Success = true,
                    Message = "Data insert successfully",
                    data = data
                });

            }
            catch(Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [Route("list")]
        [HttpGet]
        public async Task<IActionResult> GetBrands()
        {
            try
            {
                var result = await _businessLayer.GetBrands();
                return Ok(result);
            }
            catch (Exception ex) 
            { 
                return StatusCode(500,new { Message = ex.Message });
            }
        }

        [HttpGet("list/{id}")]
        public async Task<IActionResult> GetBrandsById(Guid id)
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
                var brand = await _businessLayer.GetBrandById(id);

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

        [HttpPut("update/{id}")]
        public async Task<IActionResult> UpdateBrandsById(Guid id, [FromBody] BrandInsertModel request)

        {
            string userEmail =
                User.FindFirst(ClaimTypes.Email)?.Value ??
                User.FindFirst("email")?.Value ??
                User.FindFirst("UserEmail")?.Value;

            try
            {
                var data = await _businessLayer.UpdateBrandsById(id, userEmail, request);
                return Ok(new
                {
                    Success = true,
                    Message = "Data insert successfully",
                    data = data
                });

            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
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
