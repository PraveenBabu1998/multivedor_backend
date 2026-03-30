using elemechWisetrack.BusinessLayer;
using elemechWisetrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace elemechWisetrack.Controllers
{
    [ApiController]
    [Route("api/artical")]
    [Authorize]
    public class ArticalsController : ControllerBase
    {
        private readonly IBusinessLayer _businessLayer;

        public ArticalsController(IBusinessLayer businessLayer)
        {
            _businessLayer = businessLayer;
        }

        private string GetEmail()
        {
            return User.FindFirst(ClaimTypes.Email)?.Value ??
                   User.FindFirst("UserName")?.Value ??
                   User.FindFirst("email")?.Value;
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddArtical([FromForm] ArticalModel model)
        {
            var email = GetEmail();
            var result = await _businessLayer.AddArtical(email, model);
            return Ok(result);
        }

        [HttpGet("get-all")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var result = await _businessLayer.GetAllArticals();
            return Ok(result);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _businessLayer.GetArticalById(id);
            return Ok(result);
        }

        [HttpPut("update/{id}")]
        public async Task<IActionResult> Update(Guid id, [FromForm] ArticalModel model)
        {
            var email = GetEmail();
            var result = await _businessLayer.UpdateArtical(email, id, model);
            return Ok(result);
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var email = GetEmail();
            var result = await _businessLayer.DeleteArtical(email, id);
            return Ok(result);
        }
    }
}