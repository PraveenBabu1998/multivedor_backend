using elemechWisetrack.BusinessLayer;
using elemechWisetrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace elemechWisetrack.Controllers
{
    [ApiController]
    [Route("api/blogs")]
    [Authorize]
    public class BlogsController : ControllerBase
    {
        private readonly IBusinessLayer _businessLayer;
        public BlogsController(IBusinessLayer businessLayer)
        {
            _businessLayer = businessLayer;
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddBlogs([FromForm] AddBlogDto model)
        {
            var userId = User.FindFirst(ClaimTypes.Email)?.Value;

            var result = await _businessLayer.AddBlog(model, userId);
            return Ok(result);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetBlogs()
        {
            var result = await _businessLayer.GetBlogs();
            return Ok(result);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetBlogById(string id)
        {
            var result = await _businessLayer.GetBlogById(id);
            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBlog(string id, [FromForm] AddBlogDto model)
        {
            var userId = User.FindFirst(ClaimTypes.Email)?.Value;

            var result = await _businessLayer.UpdateBlog(id, model, userId);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBlog(string id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var result = await _businessLayer.DeleteBlog(id, userId);
            return Ok(result);
        }
    }
}
