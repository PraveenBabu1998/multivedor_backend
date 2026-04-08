namespace elemechWisetrack.Models
{
    public class CategoryModel
    {
        public int Id { get; set; }
        public string CategoryName { get; set; }
        public string CategorySlug { get; set; } // new field
        public string? CategoryImage { get; set; }
        public bool CategoryStatus { get; set; }
        public int? CategoryCount { get; set; }
    }

    public class ReviewModel
    {
        public int Rating { get; set; }
        public string Comment { get; set; }
    }

    public class UpdateOrderStatusModel
    {
        public Guid OrderId { get; set; }
        public string Status { get; set; } = string.Empty;

        // ✅ NEW FIELD
        public string UpdatedByEmail { get; set; } = string.Empty;
    }

    public class OrderDetailsResponse
    {
        public Guid OrderId { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal Discount { get; set; }
        public decimal FinalAmount { get; set; }

        public string PaymentMethod { get; set; }
        public string PaymentStatus { get; set; }
        public string OrderStatus { get; set; }

        public string CouponCode { get; set; }
        public DateTime CreatedAt { get; set; }

        public PaymentModel Payment { get; set; }
        public AddressModel Address { get; set; }

        public List<OrderItemModelResponse> Items { get; set; } = new();
    }

    public class PaymentModel
    {
        public string RazorpayOrderId { get; set; }
        public string RazorpayPaymentId { get; set; }
        public string RazorpaySignature { get; set; }
        public string Status { get; set; }
        public string Method { get; set; }
    }

    public class AddressModel
    {
        public Guid AddressId { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Pincode { get; set; }
    }

    public class OrderItemModelResponse
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Discount { get; set; }

        public string ProductName { get; set; }
        public string ProductImage { get; set; }
        public string Description { get; set; }
        public decimal ProductPrice { get; set; }
    }

}
