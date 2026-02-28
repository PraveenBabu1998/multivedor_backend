using elemechWisetrack.Areas.Identity.Data;
using elemechWisetrack.BusinessLayer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

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
