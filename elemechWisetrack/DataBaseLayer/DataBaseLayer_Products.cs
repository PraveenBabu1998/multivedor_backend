using elemechWisetrack.Models;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace elemechWisetrack.DataBaseLayer
{
    public interface IDataBaseLayer_Products
    {
        Task<object> AddSingleProduct(string userEmail, [FromBody] ProductInsertModel request, string baseSlug);
        Task<object> GetAllProducts(int? page, int? pageSize);
        Task<object> UpdateProduct(
    Guid productId,
    string userEmail,
    ProductInsertModel request,
    string? baseSlug);
    }

    public partial interface IDataBaseLayer : IDataBaseLayer_Products { }

    public partial class DataBaseLayer
    {
        public async Task<object> AddSingleProduct(
    string userEmail,
    ProductInsertModel request,
    string baseSlug)
        {
            try
            {
                using (var conn = new NpgsqlConnection(DbConnection))
                {
                    await conn.OpenAsync();

                    // =============================
                    // 1️⃣ Get User Id From Email
                    // =============================
                    string getUserQuery = @"
                SELECT ""Id""
                FROM ""AspNetUsers""
                WHERE ""Email"" = @Email
                LIMIT 1;";

                    Guid userId;

                    using (var userCmd = new NpgsqlCommand(getUserQuery, conn))
                    {
                        userCmd.Parameters.AddWithValue("@Email", userEmail);

                        var result = await userCmd.ExecuteScalarAsync();

                        if (result == null)
                        {
                            return new
                            {
                                Success = false,
                                Message = "User not found"
                            };
                        }

                        userId = Guid.Parse(result.ToString());
                    }

                    // 🔥 Generate Unique Slug
                    string slug = baseSlug;

                    // 🔥 Generate SKU
                    string sku = await GenerateSku();

                    string insertQuery = @"
                INSERT INTO products (
                    Name, Slug, ShortDescription, Description,
                    CategoryId, SubCategoryId, BrandId,
                    Price, DiscountPrice, CostPrice, TaxPercentage,
                    SKU, StockQuantity, MinStockQuantity, TrackInventory,
                    MainImage, GalleryImages,
                    Weight, Length, Width, Height,
                    MetaTitle, MetaDescription, MetaKeywords,
                    IsActive, IsFeatured, IsDeleted,
                    CreatedBy
                )
                VALUES (
                    @Name, @Slug, @ShortDescription, @Description,
                    @CategoryId, @SubCategoryId, @BrandId,
                    @Price, @DiscountPrice, @CostPrice, @TaxPercentage,
                    @SKU, @StockQuantity, @MinStockQuantity, @TrackInventory,
                    @MainImage, @GalleryImages,
                    @Weight, @Length, @Width, @Height,
                    @MetaTitle, @MetaDescription, @MetaKeywords,
                    @IsActive, @IsFeatured, @IsDeleted,
                    @CreatedBy
                )
                RETURNING Id;";

                    using (var cmd = new NpgsqlCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@Name", request.Name);
                        cmd.Parameters.AddWithValue("@Slug", slug);
                        cmd.Parameters.AddWithValue("@ShortDescription", (object?)request.ShortDescription ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@CategoryId", request.CategoryId);
                        cmd.Parameters.AddWithValue("@SubCategoryId", (object?)request.SubCategoryId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@BrandId", (object?)request.BrandId ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@Price", request.Price);
                        cmd.Parameters.AddWithValue("@DiscountPrice", (object?)request.DiscountPrice ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CostPrice", (object?)request.CostPrice ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@TaxPercentage", (object?)request.TaxPercentage ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@SKU", sku);
                        cmd.Parameters.AddWithValue("@StockQuantity", request.StockQuantity);
                        cmd.Parameters.AddWithValue("@MinStockQuantity", (object?)request.MinStockQuantity ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@TrackInventory", request.TrackInventory);

                        cmd.Parameters.AddWithValue("@MainImage", (object?)request.MainImage ?? DBNull.Value);

                        // 🔥 PostgreSQL TEXT[] support
                        cmd.Parameters.AddWithValue("@GalleryImages",
                            request.GalleryImages != null && request.GalleryImages.Any()
                                ? request.GalleryImages.ToArray()
                                : DBNull.Value);

                        cmd.Parameters.AddWithValue("@Weight", (object?)request.Weight ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Length", (object?)request.Length ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Width", (object?)request.Width ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Height", (object?)request.Height ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@MetaTitle", (object?)request.MetaTitle ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@MetaDescription", (object?)request.MetaDescription ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@MetaKeywords", (object?)request.MetaKeywords ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@IsActive", request.IsActive);
                        cmd.Parameters.AddWithValue("@IsFeatured", request.IsFeatured);
                        cmd.Parameters.AddWithValue("@IsDeleted", request.IsDeleted);

                        cmd.Parameters.AddWithValue("@CreatedBy", userId);

                        var insertedId = await cmd.ExecuteScalarAsync();

                        return new
                        {
                            success = true,
                            message = "Product added successfully",
                            productId = insertedId,
                            sku = sku,
                            slug = slug
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new
                {
                    success = false,
                    message = "Error while adding product",
                    error = ex.Message
                };
            }
        }

        public async Task<object> GetAllProducts(int? page, int? pageSize)
        {
            try
            {
                // Default values
                int basePage = (page.HasValue && page.Value > 0) ? page.Value : 1;
                int basePageSize = (pageSize.HasValue && pageSize.Value > 0) ? pageSize.Value : 100;

                using (var conn = new NpgsqlConnection(DbConnection))
                {
                    await conn.OpenAsync();

                    int offset = (basePage - 1) * basePageSize;

                    // Total Count
                    string countQuery = @"SELECT COUNT(*) 
                                  FROM products 
                                  WHERE IsDeleted = FALSE;";

                    int totalRecords;
                    using (var countCmd = new NpgsqlCommand(countQuery, conn))
                    {
                        totalRecords = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
                    }

                    // Get Products
                    string getQuery = @"
                SELECT *
                FROM products
                WHERE IsDeleted = FALSE
                ORDER BY CreatedDate DESC
                LIMIT @pageSize OFFSET @offset;";

                    var products = new List<object>();

                    using (var cmd = new NpgsqlCommand(getQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@pageSize", basePageSize);
                        cmd.Parameters.AddWithValue("@offset", offset);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                products.Add(new
                                {
                                    Id = reader["Id"],
                                    Name = reader["Name"]?.ToString(),
                                    Slug = reader["Slug"]?.ToString(),
                                    ShortDescription = reader["ShortDescription"]?.ToString(),
                                    Description = reader["Description"]?.ToString(),

                                    CategoryId = reader["CategoryId"],
                                    SubCategoryId = reader["SubCategoryId"] == DBNull.Value ? null : reader["SubCategoryId"],
                                    BrandId = reader["BrandId"] == DBNull.Value ? null : reader["BrandId"],

                                    Price = reader["Price"],
                                    DiscountPrice = reader["DiscountPrice"] == DBNull.Value ? null : reader["DiscountPrice"],
                                    CostPrice = reader["CostPrice"] == DBNull.Value ? null : reader["CostPrice"],
                                    TaxPercentage = reader["TaxPercentage"] == DBNull.Value ? null : reader["TaxPercentage"],

                                    SKU = reader["SKU"]?.ToString(),
                                    StockQuantity = reader["StockQuantity"],
                                    MinStockQuantity = reader["MinStockQuantity"] == DBNull.Value ? null : reader["MinStockQuantity"],
                                    TrackInventory = reader["TrackInventory"],

                                    MainImage = reader["MainImage"]?.ToString(),
                                    GalleryImages = reader["GalleryImages"] == DBNull.Value
                                                        ? new string[] { }
                                                        : (string[])reader["GalleryImages"],

                                    Weight = reader["Weight"] == DBNull.Value ? null : reader["Weight"],
                                    Length = reader["Length"] == DBNull.Value ? null : reader["Length"],
                                    Width = reader["Width"] == DBNull.Value ? null : reader["Width"],
                                    Height = reader["Height"] == DBNull.Value ? null : reader["Height"],

                                    MetaTitle = reader["MetaTitle"]?.ToString(),
                                    MetaDescription = reader["MetaDescription"]?.ToString(),
                                    MetaKeywords = reader["MetaKeywords"]?.ToString(),

                                    IsActive = reader["IsActive"],
                                    IsFeatured = reader["IsFeatured"],
                                    IsDeleted = reader["IsDeleted"],

                                    CreatedBy = reader["CreatedBy"] == DBNull.Value ? null : reader["CreatedBy"],
                                    CreatedDate = reader["CreatedDate"]
                                });
                            }
                        }
                    }

                    return new
                    {
                        success = true,
                        page = basePage,
                        pageSize = basePageSize,
                        totalRecords,
                        totalPages = (int)Math.Ceiling((double)totalRecords / basePageSize),
                        data = products
                    };
                }
            }
            catch (Exception ex)
            {
                return new
                {
                    success = false,
                    message = "Error while fetching products",
                    error = ex.Message
                };
            }
        }

        public async Task<object> UpdateProduct(
    Guid productId,
    string userEmail,
    ProductInsertModel request,
    string? baseSlug)
        {
            try
            {
                using (var conn = new NpgsqlConnection(DbConnection))
                {
                    await conn.OpenAsync();

                    // =============================
                    // 1️⃣ Check Product Exists
                    // =============================
                    string checkQuery = @"SELECT id FROM products 
                                  WHERE id = @Id AND IsDeleted = FALSE
                                  LIMIT 1;";

                    using (var checkCmd = new NpgsqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@Id", productId);

                        var exists = await checkCmd.ExecuteScalarAsync();

                        if (exists == null)
                        {
                            return new
                            {
                                success = false,
                                message = "Product not found"
                            };
                        }
                    }

                    // =============================
                    // 2️⃣ Generate Slug (Optional)
                    // =============================
                    string slug = baseSlug ?? request.Name.ToLower().Replace(" ", "-");

                    // =============================
                    // 3️⃣ Update Query
                    // =============================
                    string updateQuery = @"
                UPDATE products SET
                    Name = @Name,
                    Slug = @Slug,
                    ShortDescription = @ShortDescription,
                    Description = @Description,

                    CategoryId = @CategoryId,
                    SubCategoryId = @SubCategoryId,
                    BrandId = @BrandId,

                    Price = @Price,
                    DiscountPrice = @DiscountPrice,
                    CostPrice = @CostPrice,
                    TaxPercentage = @TaxPercentage,

                    StockQuantity = @StockQuantity,
                    MinStockQuantity = @MinStockQuantity,
                    TrackInventory = @TrackInventory,

                    MainImage = @MainImage,
                    GalleryImages = @GalleryImages,

                    Weight = @Weight,
                    Length = @Length,
                    Width = @Width,
                    Height = @Height,

                    MetaTitle = @MetaTitle,
                    MetaDescription = @MetaDescription,
                    MetaKeywords = @MetaKeywords,

                    IsActive = @IsActive,
                    IsFeatured = @IsFeatured,
                    IsDeleted = @IsDeleted
                WHERE Id = @Id
                RETURNING Id;";

                    using (var cmd = new NpgsqlCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", productId);

                        cmd.Parameters.AddWithValue("@Name", request.Name);
                        cmd.Parameters.AddWithValue("@Slug", slug);
                        cmd.Parameters.AddWithValue("@ShortDescription", (object?)request.ShortDescription ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@CategoryId", request.CategoryId);
                        cmd.Parameters.AddWithValue("@SubCategoryId", (object?)request.SubCategoryId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@BrandId", (object?)request.BrandId ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@Price", request.Price);
                        cmd.Parameters.AddWithValue("@DiscountPrice", (object?)request.DiscountPrice ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CostPrice", (object?)request.CostPrice ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@TaxPercentage", (object?)request.TaxPercentage ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@StockQuantity", request.StockQuantity);
                        cmd.Parameters.AddWithValue("@MinStockQuantity", (object?)request.MinStockQuantity ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@TrackInventory", request.TrackInventory);

                        cmd.Parameters.AddWithValue("@MainImage", (object?)request.MainImage ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@GalleryImages",
                            request.GalleryImages != null && request.GalleryImages.Any()
                                ? request.GalleryImages.ToArray()
                                : DBNull.Value);

                        cmd.Parameters.AddWithValue("@Weight", (object?)request.Weight ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Length", (object?)request.Length ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Width", (object?)request.Width ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Height", (object?)request.Height ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@MetaTitle", (object?)request.MetaTitle ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@MetaDescription", (object?)request.MetaDescription ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@MetaKeywords", (object?)request.MetaKeywords ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@IsActive", request.IsActive);
                        cmd.Parameters.AddWithValue("@IsFeatured", request.IsFeatured);
                        cmd.Parameters.AddWithValue("@IsDeleted", request.IsDeleted);

                        var updatedId = await cmd.ExecuteScalarAsync();

                        return new
                        {
                            success = true,
                            message = "Product updated successfully",
                            productId = updatedId,
                            slug = slug
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new
                {
                    success = false,
                    message = "Error while updating product",
                    error = ex.Message
                };
            }
        }
        public async Task<string> GenerateSku()
        {
            using (var conn = new NpgsqlConnection(DbConnection))
            {
                await conn.OpenAsync();

                string prefix = "EleMech";

                string query = @"
            SELECT COALESCE(
                MAX(CAST(SUBSTRING(sku, 8) AS INTEGER)), 0
            )
            FROM products
            WHERE sku LIKE @prefix";

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@prefix", prefix + "%");

                    var result = await cmd.ExecuteScalarAsync();
                    int nextNumber = Convert.ToInt32(result) + 1;

                    return prefix + nextNumber.ToString("D7");
                }
            }
        }
    }
}
