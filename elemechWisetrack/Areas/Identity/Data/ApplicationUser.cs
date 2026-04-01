using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace elemechWisetrack.Areas.Identity.Data;

// Add profile data for application users by adding properties to the ApplicationUser class
public class ApplicationUser : IdentityUser
{
    public bool IsActive { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime CreateDate { get; set; }
    public string? AccessKey { get; set; }
    public string? Lastotp { get; set; }
    public long PasswordChangeTime { get; set; }
    public string? PasswordChangeBy { get; set; }
    public string? OrgId { get; set; }
    public int userType { get; set; }
    public string? signupsource { get; set; }
    public string? sourcetype { get; set; }
    public string? address { get; set; }
    public string? email { get; set; } = null;
    public string? PhoneNumber { get; set; }

    public bool IsVendor { get; set; } = false;
    public string Status { get; set; } = "pending"; // pending / approved / rejected

    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedBy { get; set; }
    public string? RejectionReason { get; set; }

}

