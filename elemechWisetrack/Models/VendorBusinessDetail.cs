using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace elemechWisetrack.Models
{
    [Table("VendorBusinessDetails")]
    public class VendorBusinessDetails
    {
        [Key]
        public Guid Id { get; set; }

        public string? VendorId { get; set; }

        // Business Info
        public string? BusinessName { get; set; }
        public string? BusinessType { get; set; }
        public string? BusinessCategory { get; set; }
        public string? BusinessDescription { get; set; }

        // Registration
        public string? GSTNumber { get; set; }
        public string? PANNumber { get; set; }
        public string? CINNumber { get; set; }
        public string? UdyamRegistrationNumber { get; set; }

        // Address
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; } = "India";
        public string? Pincode { get; set; }

        // Contact
        public string? BusinessEmail { get; set; }
        public string? BusinessPhone { get; set; }
        public string? AlternatePhone { get; set; }
        public string? WebsiteUrl { get; set; }

        // Documents
        public string? GSTDocumentUrl { get; set; }
        public string? PANDocumentUrl { get; set; }
        public string? CINCertificateUrl { get; set; }
        public string? BusinessLogoUrl { get; set; }

        // Status
        public bool IsVerified { get; set; } = false;
        public bool IsActive { get; set; } = true;

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }

    public class VendorReviewRemarkRequest
    {
        public string? Message { get; set; }
    }
}
