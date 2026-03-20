using elemechWisetrack.Areas.Identity.Data;
using elemechWisetrack.BusinessLayer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace elemechWisetrack.Controllers
{
    [EnableCors("CorsPolicy")]
    [Route("api/category")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SUPERADMIN,ADMIN")]
    public class CategoryController : ControllerBase
    {
        private readonly IBusinessLayer _businessLayer;
        private UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<AdminController> _logger;

        public CategoryController(IBusinessLayer businessLayer, SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ILogger<AdminController> logger, IConfiguration configuration)
        {
            this._businessLayer = businessLayer;
            this._userManager = userManager;
            this._signInManager = signInManager;
            this._roleManager = roleManager;
            _logger = logger;
        }
        [Route("insert")]
        [HttpPost]
        public async Task<IActionResult> UpLoadCategory()
        {
            try
            {
                var form = await Request.ReadFormAsync();
                var result = await _businessLayer.UploadCategory(form);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }

        }

        [Route("list")]
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ListCategory()
        {
            try
            {
                var data = await _businessLayer.GetCategoryTree();

                return Ok(new
                {
                    success = true,
                    message = "Data listing successfully",
                    data = data
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

        [Route("list/{categoryId}")]
        [HttpGet]
        public async Task<IActionResult> ListCategoryById(Guid categoryId)
        {
            try
            {
                var data = await _businessLayer.ListCategoryById(categoryId);

                return Ok(new
                {
                    success = true,
                    message = "Data listing successfully",
                    data = data
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

        [Route("update/{categoryId}")]
        [HttpPut]
        public async Task<IActionResult> UpdateCategory(Guid categoryId)
        {
            try
            {
                var form = await Request.ReadFormAsync();
                var result = await _businessLayer.UpdateCategory(categoryId,form);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }

        }

        [Route("delete/{categoryId}")]
        [HttpDelete]
        public async Task<IActionResult> DeleteCategory(Guid categoryId)
        {
            try
            {
                var result = await _businessLayer.DeleteCategory(categoryId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }

        }

    }
}
