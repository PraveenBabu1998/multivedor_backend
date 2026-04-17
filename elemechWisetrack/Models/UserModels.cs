using System.ComponentModel.DataAnnotations;

namespace elemechWisetrack.Models
{
    public class ResetPasswordModel
    {
        [Microsoft.Build.Framework.Required] public string UserEmail { get; set; }
        [Microsoft.Build.Framework.Required] public string Token { get; set; }

        [Microsoft.Build.Framework.Required, DataType(DataType.Password)]
        public string NewPassword { get; set; }

        [Microsoft.Build.Framework.Required, DataType(DataType.Password)]
        [Compare("NewPassword")]
        public string ConfirmPassword { get; set; }

        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public string ChangedBy { get; set; }
    }



    public class CategoryTreeDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public string? Image { get; set; }
        public bool IsActive { get; set; }
        public Guid? ParentId { get; set; }
        public List<CategoryTreeDto> Children { get; set; } = new();
    }

    public class ProductInsertModel
    {
        // Basic Info
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? ShortDescription { get; set; }
        public string? Description { get; set; }

        // Category & Brand
        public Guid CategoryId { get; set; }
        public Guid? SubCategoryId { get; set; }
        public Guid? BrandId { get; set; }
        public Guid? ColorId { get; set; }
        public Guid? SizeId { get; set; }
        public List<Guid>? ColorIds { get; set; }
        public List<Guid>? SizeIds { get; set; }

        // Pricing
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public decimal? CostPrice { get; set; }
        public decimal? TaxPercentage { get; set; }

        // Inventory
        public string SKU { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
        public int? MinStockQuantity { get; set; }
        public bool TrackInventory { get; set; } = true;

        // ✅ Product Images (FILE UPLOAD)
        public IFormFile? MainImage { get; set; }

        public List<IFormFile>? GalleryImages { get; set; }

        // Physical Details
        public decimal? Weight { get; set; }
        public decimal? Length { get; set; }
        public decimal? Width { get; set; }
        public decimal? Height { get; set; }

        // SEO
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public string? MetaKeywords { get; set; }

        // Status
        public bool IsActive { get; set; } = true;
        public bool IsFeatured { get; set; } = false;
        public bool IsDeleted { get; set; } = false;
    }

    //public class BrandInsertModel
    //{
    //    public string Name { get; set; } = string.Empty;

    //    public string Slug { get; set; } = string.Empty;

    //    public string? Description { get; set; }

    //    public string? Logo { get; set; }

    //    public bool IsActive { get; set; } = true;

    //    public Guid CreatedBy { get; set; }
    //}

    public class BrandModel
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Slug { get; set; }
        public string? Description { get; set; }

        public string? Logo { get; set; }   // ✅ string (image path)

        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class BrandInsertModel
    {
        public string Name { get; set; }
        public string? Description { get; set; }

        // ✅ file upload
        public IFormFile? Logo { get; set; }

        public bool IsActive { get; set; }
    }

    public class ProductsCollors
    {
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? HexCode { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class ProductSizes
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class ProductSizeRequest
    {
        public Guid ProductId { get; set; }
        public Guid SizeId { get; set; }
    }

    public class ProductColorRequest
    {
        public Guid ProductId { get; set; }
        public Guid ColorId { get; set; }
    }

    public class ExcelProductRow
    {
        public string B { get; set; }
        public string E { get; set; }
        public string G { get; set; }
        public string H { get; set; }
        public string I { get; set; }
        public string M { get; set; }
        public string P { get; set; }
        public string Q { get; set; }
        public string T { get; set; }
        public string U { get; set; }
        public string V { get; set; }
        public string W { get; set; }
        public string X { get; set; }
        public string Y { get; set; }
        public string AA { get; set; }
    }

    public class ExcelProductsRow
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public string Brand { get; set; }
        public string ShortDescription { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string SKU { get; set; }
        public string Images { get; set; }   // image1.jpg,image2.jpg,image3.jpg
        public string ProductCode { get; set; }
        public string Specification { get; set; }
    }

    public class AddBlogDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Content { get; set; }
        public IFormFile? Image { get; set; }
        public string MetaTitle { get; set; }
        public string MetaDescription { get; set; }
        public List<string> Tags { get; set; }
        public string Status { get; set; }
    }


    public class ContactUsModel
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
    }

    public class ApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
    }

}
