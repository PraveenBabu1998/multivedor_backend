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

    public class CartItemResponse
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Total => Quantity * Price;
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
}