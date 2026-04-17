using Azure.Core;
using elemechWisetrack.BusinessLayer;
using elemechWisetrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace elemechWisetrack.Controllers
{
    [ApiController]
    [Route("api/colors")]
    [Authorize]

    public class ColorsController : ControllerBase
    {
        private readonly IBusinessLayer _businessLayer;
        public ColorsController(IBusinessLayer businessLayer)
        {
            _businessLayer = businessLayer;
        }

        [Route("insert")]
        [HttpPost]
        public async Task<IActionResult> AddColors([FromBody] ProductsCollors request)
        {
            try
            {
                string userEmail = User.FindFirst(ClaimTypes.Email)?.Value ??
                                   User.FindFirst("UserName")?.Value ??
                                   User.FindFirst("email")?.Value;

                var result = await _businessLayer.AddColors(userEmail, request);

                return Ok(result);   // ✅ Now returning IActionResult
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Route("get")]
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetColors()
        {
            try
            {
                string userEmail = User.FindFirst(ClaimTypes.Email)?.Value ??
                                   User.FindFirst("UserName")?.Value ??
                                   User.FindFirst("email")?.Value;

                var result = await _businessLayer.GetColors();

                return Ok(result);   // ✅ Now returning IActionResult
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPut("update-color/{id}")]
        public async Task<IActionResult> UpdateColor(Guid id, [FromBody] ProductsCollors request)
        {
            try
            {
                string userEmail = User.FindFirst(ClaimTypes.Email)?.Value ??
                                   User.FindFirst("UserName")?.Value ??
                                   User.FindFirst("email")?.Value;

                var result = await _businessLayer.UpdateColor(userEmail, id, request);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        [HttpPatch("toggle-color/{id}")]
        public async Task<IActionResult> ToggleColor(Guid id)
        {
            try
            {
                var result = await _businessLayer.ToggleColorStatus(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }


        [HttpDelete("soft-delete-color/{id}")]
        public async Task<IActionResult> SoftDeleteColor(Guid id)
        {
            try
            {
                var result = await _businessLayer.SoftDeleteColor(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        [HttpDelete("delete-color/{id}")]
        public async Task<IActionResult> DeleteColor(Guid id)
        {
            var result = await _businessLayer.DeleteColor(id);
            return Ok(result);
        }

    }
}
