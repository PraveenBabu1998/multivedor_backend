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
    }
}
