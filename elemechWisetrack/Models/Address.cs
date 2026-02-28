using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace elemechWisetrack.Models
{
[Table("address_details")]
    public class AddressDetails
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }

        public string FullName { get; set; }
        public string PhoneNumber { get; set; }

        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }

        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string PostalCode { get; set; }

        public string AddressType { get; set; }
        public bool IsDefault { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }


    public class AddressRequest
    {
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string PostalCode { get; set; }
        public string AddressType { get; set; }
        public bool IsDefault { get; set; }
    }

    public class AppUser
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string Password { get; set; }
    }



}
