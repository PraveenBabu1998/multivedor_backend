using elemechWisetrack.Areas.Identity.Data;
using Microsoft.AspNetCore.Identity;

namespace elemechWisetrack.BusinessLayer
{
    public interface IBusinessLayer_Admin
    {
        Task<object> AddUser(IFormCollection form);
        Task<object> RegisterVendor(IFormCollection form);
        Task<object> ApproveVendor(string userId, string adminId);
        Task<object> RejectVendor(string userId, string adminId, string reason);
        Task<object> GetAdminVendors();
        Task<object> GetAllUsersByRole();
        Task<object> AddVendorReviewRemark(string userId, string adminEmail, string message);
        Task<object> GetVendorDetailsById(string userId);
    }

    public partial interface IBusinessLayer : IBusinessLayer_Admin
    {

    }

    public partial class BusinessLayer
    {
        public async Task<object> AddUser(IFormCollection form)
        {
            bool success = false;
            string message = string.Empty;

            if (_roleManager == null)
                return new { success = false, message = "_roleManager is null – check dependency injection" };

            if (_userManager == null)
                return new { success = false, message = "_userManager is null – check dependency injection" };

            try
            {
                // 🟢 Get form fields
                string email = form["email"];
                string password = form["password"]; // ⬅️ use form password directly
                string firstname = form["firstname"];
                string lastname = form["lastname"];
                string phone = form["phone"];
                var roles = form["role"].ToList();

                // 🟢 Basic validation
                if (string.IsNullOrEmpty(email) ||
                    string.IsNullOrEmpty(password) ||
                    string.IsNullOrEmpty(firstname) ||
                    string.IsNullOrEmpty(lastname) ||
                    string.IsNullOrEmpty(phone) ||
                    !roles.Any())
                {
                    return new { success = false, message = "One or more required fields are missing." };
                }

                // 🟢 Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(email);
                if (existingUser != null)
                {
                    return new { success = false, message = "Email address already exists." };
                }

                // 🟢 Create new user
                var user = new ApplicationUser
                {
                    Email = email,
                    UserName = email,
                    FirstName = firstname,
                    LastName = lastname,
                    PhoneNumber = phone,
                    EmailConfirmed = true,
                    IsActive = true,
                    CreateDate = DateTime.UtcNow,
                    userType = 1,
                    sourcetype = "internal"
                };

                var result = await _userManager.CreateAsync(user, password);

                if (result.Succeeded)
                {
                    var availableRoles = _roleManager.Roles.Select(r => r.Name).ToList();

                    foreach (var role in roles)
                    {
                        if (!string.IsNullOrEmpty(role) &&
                            availableRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
                        {
                            await _userManager.AddToRoleAsync(user, role);
                        }
                    }

                    success = true;
                    message = "User created successfully.";
                }
                else
                {
                    message = string.Join(", ", result.Errors.Select(e => e.Description));
                }
            }
            catch (Exception ex)
            {
                success = false;
                message = $"Error: {ex.Message}";
            }

            return new { success, message };
        }

        public async Task<object> RegisterVendor(IFormCollection form)
        {
            bool success = false;
            string message = string.Empty;

            try
            {
                string email = form["email"];
                string password = form["password"];
                string firstname = form["firstname"];
                string lastname = form["lastname"];
                string phone = form["phone"];

                if (string.IsNullOrEmpty(email) ||
                    string.IsNullOrEmpty(password) ||
                    string.IsNullOrEmpty(firstname) ||
                    string.IsNullOrEmpty(lastname) ||
                    string.IsNullOrEmpty(phone))
                {
                    return new { success = false, message = "Required fields: email, password, firstname, lastname, phone" };
                }

                // 🔹 Check existing user
                var existingUser = await _userManager.FindByEmailAsync(email);
                if (existingUser != null)
                {
                    return new { success = false, message = "Email already exists" };
                }

                // 🔹 Create Vendor User
                var user = new ApplicationUser
                {
                    Email = email,
                    UserName = email,
                    FirstName = firstname,
                    LastName = lastname,
                    PhoneNumber = phone,
                    EmailConfirmed = true,
                    IsActive = true,

                    // ⭐ Vendor specific
                    IsVendor = true,
                    Status = "pending",

                    CreateDate = DateTime.UtcNow,
                    userType = 2, // optional: 2 = vendor
                    sourcetype = "vendor"
                };

                var result = await _userManager.CreateAsync(user, password);

                if (!result.Succeeded)
                {
                    return new
                    {
                        success = false,
                        message = string.Join(", ", result.Errors.Select(e => e.Description))
                    };
                }

                // 🔹 Assign Vendor Role
                if (!await _roleManager.RoleExistsAsync("ADMIN"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("ADMIN"));
                }

                await _userManager.AddToRoleAsync(user, "ADMIN");

                success = true;
                message = "Vendor registered successfully. Waiting for admin approval.";
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            return new { success, message };
        }

        public async Task<object> ApproveVendor(string userId, string adminEmail)
        {
            return await _dataBaseLayer.ApproveVendor(userId, adminEmail);
        }

        public async Task<object> RejectVendor(string userId, string adminEmail, string reason)
        {
            
            return await _dataBaseLayer.RejectVendor(userId, adminEmail, reason);
        }

        public async Task<object> GetAdminVendors()
        {
            return await _dataBaseLayer.GetAllVendors();
        }
        public async Task<object> GetAllUsersByRole()
        {
            return await _dataBaseLayer.GetAllUsersByRole();
        }

        public async Task<object> AddVendorReviewRemark(string userId, string adminEmail, string message)
        {
            return await _dataBaseLayer.AddVendorReviewRemark(userId, adminEmail, message);
        }

        public async Task<object> GetVendorDetailsById(string userId)
        {
            return await _dataBaseLayer.GetVendorDetailsById(userId);
        }
    }
}
