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
        public decimal CurrentPrice { get; set; }
        public decimal Total { get; set; }
    }

    public class CreateOrderModel
    {
        public Guid AddressId { get; set; }
        public string PaymentMethod { get; set; } // COD / RAZORPAY
        public Guid CartId { get; set; }
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
}