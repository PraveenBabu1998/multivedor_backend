using elemechWisetrack.Areas.Identity.Data;
using elemechWisetrack.BusinessLayer;
using elemechWisetrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Primitives;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace elemechWisetrack.Controllers
{
    public class AdminController : ControllerBase
    {
        private readonly IBusinessLayer _businessLayer;
        private UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<AdminController> _logger;

        public AdminController(IBusinessLayer businessLayer, SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ILogger<AdminController> logger, IConfiguration configuration)
        {
            this._businessLayer = businessLayer;
            this._userManager = userManager;
            this._signInManager = signInManager;
            this._roleManager = roleManager;
            _logger = logger;
        }

        [Route("register")]
        [HttpPost]
        public async Task<IActionResult> AddUser(IFormCollection form)
        {
            var result = await _businessLayer.AddUser(form);
            return Ok(result);
        }

        [Route("register-vendor")]
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> RegisterVendor()
        {
            Request.EnableBuffering();
            IFormCollection form;

            // 1) Try standard form read first (multipart/form-data or x-www-form-urlencoded)
            try
            {
                form = await Request.ReadFormAsync();
                if (form != null && form.Count > 0)
                {
                    var registerResult = await _businessLayer.RegisterVendor(form);
                    return Ok(registerResult);
                }
            }
            catch
            {
                // ignore and try other parsers
            }

            Request.Body.Position = 0;

            // 2) Optional support: JSON payload
            if (Request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true)
            {
                using var reader = new StreamReader(Request.Body);
                var body = await reader.ReadToEndAsync();

                if (string.IsNullOrWhiteSpace(body))
                    return BadRequest(new { success = false, message = "Request body is required." });

                Dictionary<string, string>? data;
                try
                {
                    data = JsonSerializer.Deserialize<Dictionary<string, string>>(body,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch
                {
                    return BadRequest(new { success = false, message = "Invalid JSON body." });
                }

                if (data == null || data.Count == 0)
                    return BadRequest(new { success = false, message = "JSON body is empty." });

                var dict = data.ToDictionary(kv => kv.Key, kv => new StringValues(kv.Value ?? string.Empty));
                form = new FormCollection(dict);
            }
            else
            {
                Request.Body.Position = 0;
                // 3) Fallback: raw form body without proper content-type.
                using var reader = new StreamReader(Request.Body);
                var rawBody = await reader.ReadToEndAsync();

                if (!string.IsNullOrWhiteSpace(rawBody))
                {
                    var parsed = QueryHelpers.ParseQuery(rawBody);
                    if (parsed.Count > 0)
                    {
                        var dict = parsed.ToDictionary(kv => kv.Key, kv => kv.Value);
                        form = new FormCollection(dict);
                    }
                    else
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "Unsupported Content-Type. Use multipart/form-data."
                        });
                    }
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Unsupported Content-Type. Use multipart/form-data."
                    });
                }
            }

            var registerResultFinal = await _businessLayer.RegisterVendor(form);
            return Ok(registerResultFinal);
        }

        [Authorize(Roles = "SUPERADMIN")]
        [HttpPost("approve-vendor/{id}")]
        public async Task<IActionResult> ApproveVendor(string id)
        {
            var adminId = User.FindFirstValue(ClaimTypes.Email);

            var result = await _businessLayer.ApproveVendor(id, adminId);

            return Ok(result);
        }

        [Authorize(Roles = "SUPERADMIN")]
        [HttpPost("reject-vendor/{id}")]
        public async Task<IActionResult> RejectVendor(string id)
        {
            var form = await Request.ReadFormAsync();
            string reason = form["reason"];

            var adminEmail = User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(adminEmail))
            {
                return Unauthorized(new { success = false, message = "Admin email missing in token" });
            }

            var result = await _businessLayer.RejectVendor(id, adminEmail, reason);

            return Ok(result);
        }

        [Authorize(Roles = "SUPERADMIN")]
        [HttpGet("admin-vendors")]
        public async Task<IActionResult> GetAdminVendors()
        {
            var result = await _businessLayer.GetAdminVendors();
            return Ok(result);
        }

        [Authorize(Roles = "SUPERADMIN")]
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var result = await _businessLayer.GetAllUsersByRole();
            return Ok(result);
        }

        [Authorize(Roles = "SUPERADMIN")]
        [HttpPost("vendor-review/{id}")]
        public async Task<IActionResult> AddVendorReview(string id, [FromBody] VendorReviewRemarkRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
                return BadRequest(new { success = false, message = "Review message is required" });

            var adminEmail = User.FindFirstValue(ClaimTypes.Email);
            var result = await _businessLayer.AddVendorReviewRemark(id, adminEmail ?? string.Empty, request.Message);
            return Ok(result);
        }

        [Authorize(Roles = "SUPERADMIN")]
        [HttpGet("vendor-details/{id}")]
        public async Task<IActionResult> GetVendorDetailsById(string id)
        {
            var result = await _businessLayer.GetVendorDetailsById(id);
            return Ok(result);
        }


        [Route("login")]
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Login()
        {
            try
            {
                _logger.LogInformation("Login endpoint called");
                var form = await Request.ReadFormAsync();
                if (form == null)
                {
                    _logger.LogWarning("Form data is null");
                    return Unauthorized(new { success = false, message = "Form data is required" });
                }
                string username = form["email"];
                string password = form["password"];
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    _logger.LogWarning("Email or password missing");
                    return Unauthorized(new { success = false, message = "Email and password are required" });
                }
                var user = await AuthenticateUser(username, password, "user");
                if (user == null)
                {
                    _logger.LogWarning("Invalid credentials for email: {Email}", username);
                    return Unauthorized(new { success = false, message = "Invalid credentials" });
                }
                var tokenString = await GenerateJSONWebToken(user);
                return Ok(new { success = true, token = tokenString.Item1 });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return StatusCode(500, new { success = false, message = $"Error during login: {ex.Message}" });
            }
        }

        private async Task<ApplicationUser> AuthenticateUser(string email, string password, string app)
        {
            _logger.LogInformation("Authenticating user with email: {Email}", email);
            ApplicationUser appUser = await _userManager.FindByNameAsync(email);
            if (appUser != null && appUser.IsActive)
            {
                var result = await _signInManager.PasswordSignInAsync(appUser, password, false, false);
                return result.Succeeded ? appUser : null;
            }
            _logger.LogWarning("User not found or inactive for email: {Email}", email);
            return null;
        }
        private async Task<Tuple<string, string>> GenerateJSONWebToken(ApplicationUser userInfo)
        {
            _logger.LogInformation("Generating JWT for user: {Email}", userInfo.Email);
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Name, userInfo.FirstName),
                new Claim(JwtRegisteredClaimNames.Email, userInfo.Email),
                new Claim("UserName", userInfo.UserName),
                new Claim("UserEmail", userInfo.Email),
                new Claim("FirstName", userInfo.FirstName),
                new Claim("LastName", userInfo.LastName),
                new Claim("UserOrg", userInfo.OrgId),
                new Claim("AccessKey", userInfo.AccessKey),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };
            var userRoles = await _userManager.GetRolesAsync(userInfo);
            foreach (var userRole in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, userRole));
            }
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Issuer"],
                claims: claims,
                expires: DateTime.Now.AddYears(1),
                signingCredentials: credentials);
            var refreshToken = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Issuer"],
                claims: claims,
                expires: DateTime.Now.AddDays(7),
                signingCredentials: credentials);
            var accessTokenString = new JwtSecurityTokenHandler().WriteToken(token);
            var refreshTokenString = new JwtSecurityTokenHandler().WriteToken(refreshToken);
            _logger.LogInformation("JWT generated successfully for user: {Email}", userInfo.Email);
            return new Tuple<string, string>(accessTokenString, refreshTokenString);
        }
    }
}
