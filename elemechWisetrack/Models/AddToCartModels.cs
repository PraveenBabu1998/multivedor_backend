namespace elemechWisetrack.Models
{
    public class AddToCartModel
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

    public class UpdateCartModel
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class RemoveCartModel
    {
        public Guid ProductId { get; set; }
    }

    public class CartResponseModel
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductImage { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        //public decimal Discountprice { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal Total { get; set; }
        public decimal Discount { get; set; } = 0;
    }

    public class CouponModel
    {
        public string DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal MinOrderAmount { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
    }

    public class UpdateCartQuantityModel
    {
        public Guid ProductId { get; set; }
        public string Action { get; set; } // ADD / REMOVE
    }

    //public class CreateOrderModel
    //{
    //    public Guid AddressId { get; set; }
    //    public string PaymentMethod { get; set; } // COD / RAZORPAY
    //    public Guid CartId { get; set; }
    //}

    public class CreateOrderItemModel
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }
    public class CreateOrderModel
    {
        public Guid AddressId { get; set; }
        public string PaymentMethod { get; set; }

        public string? CouponCode { get; set; }

        public List<CreateOrderItemModel> Items { get; set; }
    }
    public class RazorpayVerifyModel
    {
        public string RazorpayOrderId { get; set; }
        public string RazorpayPaymentId { get; set; }
        public string RazorpaySignature { get; set; }
    }

    public class ExchangeRequestModel
    {
        public Guid OrderId { get; set; }
        public Guid OrderItemId { get; set; }
        public Guid NewProductId { get; set; }
        public string Reason { get; set; }
    }

    public class UpdateExchangeStatusModel
    {
        public Guid ExchangeId { get; set; }
        public string Status { get; set; } // APPROVED / REJECTED
    }

    public class PickupRequestModel
    {
        public Guid ExchangeId { get; set; }
        public DateTime PickupDate { get; set; }
        public string PickupAddress { get; set; }
    }

    public class PickupStatusUpdateModel
    {
        public Guid ExchangeId { get; set; }
        public string Status { get; set; }
    }

    public class OrderItemModel
    {
        public Guid ProductId { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }

        public decimal Discount { get; set; } = 0; // ✅ IMPORTANT

        // ✅ GST Fields
        public decimal GstRate { get; set; }
        public decimal CGST { get; set; }
        public decimal SGST { get; set; }
        public decimal IGST { get; set; }
        public decimal GstAmount { get; set; }
    }

    public class CouponCodeCreate
    {
        public string Code { get; set; }
        public string Description { get; set; }

        public string DiscountType { get; set; } // percentage / fixed
        public decimal DiscountValue { get; set; }

        public decimal MinOrderAmount { get; set; }
        public decimal? MaxDiscountAmount { get; set; }

        public int? UsageLimit { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public string ApplicableOn { get; set; } // all / product

        public List<Guid>? ProductIds { get; set; } // optional
    }

    public class ApplyCouponRequest
    {
        public string CouponCode { get; set; }
        public decimal CartAmount { get; set; }
        public List<Guid> ProductIds { get; set; }
    }

    public class DeliveryPincode
    {
        public Guid Id { get; set; }
        public string Pincode { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public bool IsServiceable { get; set; }
        public int DeliveryDays { get; set; }
        public string CreatedByEmail { get; set; }   // ✅ NEW
    }

    public class AddPincodeRequest
    {
        public string Pincode { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public bool IsServiceable { get; set; }
        public int DeliveryDays { get; set; }
    }


    public class ArticalModel
    {
        public Guid Id { get; set; }

        public string Title { get; set; }

        public string? Slug { get; set; }      // ✅ ADD THIS

        public string Description { get; set; }

        public string Content { get; set; }

        public string? ImageUrl { get; set; }  // ✅ ADD THIS (for DB)

        public IFormFile? Image { get; set; }  // ✅ for upload
    }

    public class BannerModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Image { get; set; }
        public string? Link { get; set; }
        public bool IsActive { get; set; }
    }

    // 👉 For Create (FORM-DATA)
    public class CreateBannerModel
    {
        public string Title { get; set; }
        public IFormFile Image { get; set; }
        public string? Link { get; set; }
    }

    // 👉 For Update (FORM-DATA)
    public class UpdateBannerModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public IFormFile Image { get; set; } // optional
        public string? Link { get; set; }
        public bool IsActive { get; set; }
    }

    // 👉 DB Models
    public class CreateBannerDbModel
    {
        public string Title { get; set; }
        public string Image { get; set; }
        public string? Link { get; set; } = null;  // ✅ optional
    }

    public class UpdateBannerDbModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Image { get; set; }
        public string? Link { get; set; } = null;  // ✅ optional
        public bool IsActive { get; set; }
    }

    public class CheckoutDetailsResponse
    {
        public Guid UserId { get; set; }
        public string Email { get; set; }
        public List<CartProductDetail> CartItems { get; set; }
        public List<UserAddressDetail> Addresses { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class CartProductDetail
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string? Image { get; set; }
        public decimal Discount { get; set; } = 0; // ✅ REQUIRED
    }

    public class UserAddressDetail
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

    public class CheckoutRequest
    {
        public string? CouponCode { get; set; }
    }

    public class CartRequest
    {
        public string CouponCode { get; set; }
    }
}