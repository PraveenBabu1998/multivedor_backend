using elemechWisetrack.Models;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace elemechWisetrack.DataBaseLayer
{
    public interface IDataBaseLayer_Products
    {
        Task<object> AddSingleProduct(
    string userEmail,
    ProductInsertModel request,
    string baseSlug);
        Task<object> GetAllProducts(
    int? page, int? pageSize,
    Guid? categoryId,
    Guid? subCategoryId,
    Guid? childCategoryId,
    Guid? brandId,
    string[]? colors,
    string[]? sizes,
    decimal? minPrice,
    decimal? maxPrice,
    string? search
);
        Task<object> GetProductById(Guid productId);
        Task<object> GetAllProductsOfAdmin(string userEmail, int? page, int? pageSize);
        Task<object> UpdateProduct(
    Guid productId,
    string userEmail,
    ProductInsertModel request,
    string? baseSlug);

        Task<object> DeleteProduct(Guid productId);
        Task<object> RestoreProduct(Guid productId);
        Task<object> PermanentDeleteProduct(Guid productId);

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
                    // 1️⃣ Get User Id
                    // =============================
                    string getUserQuery = @"SELECT ""Id"" FROM ""AspNetUsers"" 
                                    WHERE ""Email""=@Email LIMIT 1";

                    Guid userId;

                    using (var userCmd = new NpgsqlCommand(getUserQuery, conn))
                    {
                        userCmd.Parameters.AddWithValue("@Email", userEmail);

                        var result = await userCmd.ExecuteScalarAsync();

                        if (result == null)
                        {
                            return new { success = false, message = "User not found" };
                        }

                        userId = Guid.Parse(result.ToString());
                    }

                    // =============================
                    // 2️⃣ Generate SKU
                    // =============================
                    string sku = "SKU-" + DateTime.Now.Ticks;

                    string slug = baseSlug;

                    // =============================
                    // 3️⃣ Upload Main Image
                    // =============================
                    string mainImagePath = null;

                    if (request.MainImage != null)
                    {
                        var folder = Path.Combine(Directory.GetCurrentDirectory(),
                            "wwwroot/uploads/products");

                        if (!Directory.Exists(folder))
                            Directory.CreateDirectory(folder);

                        var fileName = Guid.NewGuid() +
                                       Path.GetExtension(request.MainImage.FileName);

                        var filePath = Path.Combine(folder, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await request.MainImage.CopyToAsync(stream);
                        }

                        mainImagePath = "/uploads/products/" + fileName;
                    }

                    // =============================
                    // 4️⃣ Upload Gallery Images
                    // =============================
                    List<string> galleryPaths = new();

                    if (request.GalleryImages != null)
                    {
                        foreach (var image in request.GalleryImages)
                        {
                            var fileName = Guid.NewGuid() +
                                           Path.GetExtension(image.FileName);

                            var filePath = Path.Combine(
                                Directory.GetCurrentDirectory(),
                                "wwwroot/uploads/products",
                                fileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await image.CopyToAsync(stream);
                            }

                            galleryPaths.Add("/uploads/products/" + fileName);
                        }
                    }

                    // =============================
                    // 5️⃣ Insert Product
                    // =============================
                    string insertQuery = @"
            INSERT INTO products (
                name, slug, shortdescription, description,
                categoryid, subcategoryid, brandid,
                price, discountprice, costprice, taxpercentage,
                sku, stockquantity, minstockquantity, trackinventory,
                mainimage, galleryimages,
                weight, length, width, height,
                metatitle, metadescription, metakeywords,
                isactive, isfeatured, isdeleted,
                createdby
            )
            VALUES (
                @Name,@Slug,@ShortDescription,@Description,
                @CategoryId,@SubCategoryId,@BrandId,
                @Price,@DiscountPrice,@CostPrice,@TaxPercentage,
                @SKU,@StockQuantity,@MinStockQuantity,@TrackInventory,
                @MainImage,@GalleryImages,
                @Weight,@Length,@Width,@Height,
                @MetaTitle,@MetaDescription,@MetaKeywords,
                @IsActive,@IsFeatured,@IsDeleted,
                @CreatedBy
            )
            RETURNING id;";

                    using (var cmd = new NpgsqlCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@Name", request.Name);
                        cmd.Parameters.AddWithValue("@Slug", slug);

                        cmd.Parameters.AddWithValue("@ShortDescription",
                            (object?)request.ShortDescription ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@Description",
                            (object?)request.Description ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@CategoryId", request.CategoryId);

                        cmd.Parameters.AddWithValue("@SubCategoryId",
                            (object?)request.SubCategoryId ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@BrandId",
                            (object?)request.BrandId ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@Price", request.Price);

                        cmd.Parameters.AddWithValue("@DiscountPrice",
                            (object?)request.DiscountPrice ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@CostPrice",
                            (object?)request.CostPrice ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@TaxPercentage",
                            (object?)request.TaxPercentage ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@SKU", sku);

                        cmd.Parameters.AddWithValue("@StockQuantity", request.StockQuantity);

                        cmd.Parameters.AddWithValue("@MinStockQuantity",
                            (object?)request.MinStockQuantity ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@TrackInventory", request.TrackInventory);

                        cmd.Parameters.AddWithValue("@MainImage",
                            (object?)mainImagePath ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@GalleryImages",
                            galleryPaths.Any() ? galleryPaths.ToArray() : DBNull.Value);

                        cmd.Parameters.AddWithValue("@Weight",
                            (object?)request.Weight ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@Length",
                            (object?)request.Length ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@Width",
                            (object?)request.Width ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@Height",
                            (object?)request.Height ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@MetaTitle",
                            (object?)request.MetaTitle ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@MetaDescription",
                            (object?)request.MetaDescription ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@MetaKeywords",
                            (object?)request.MetaKeywords ?? DBNull.Value);

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
                    message = ex.Message
                };
            }
        }

        public async Task<object> GetAllProducts(
    int? page, int? pageSize,
    Guid? categoryId,
    Guid? subCategoryId,
    Guid? childCategoryId,
    Guid? brandId,
    string[]? colors,
    string[]? sizes,
    decimal? minPrice,
    decimal? maxPrice,
    string? search
)
        {
            try
            {
                int basePage = page ?? 1;
                int basePageSize = pageSize ?? 10;
                int offset = (basePage - 1) * basePageSize;

                using var conn = new NpgsqlConnection(DbConnection);
                await conn.OpenAsync();

                string where = "WHERE p.isdeleted = FALSE ";
                var parameters = new List<NpgsqlParameter>();

                // ============================
                // 🔹 FILTERS
                // ============================

                if (categoryId.HasValue)
                {
                    where += " AND p.categoryid = @categoryId ";
                    parameters.Add(new NpgsqlParameter("@categoryId", categoryId));
                }

                if (subCategoryId.HasValue)
                {
                    where += " AND p.subcategoryid = @subCategoryId ";
                    parameters.Add(new NpgsqlParameter("@subCategoryId", subCategoryId));
                }

                if (brandId.HasValue)
                {
                    where += " AND p.brandid = @brandId ";
                    parameters.Add(new NpgsqlParameter("@brandId", brandId));
                }

                if (colors != null && colors.Length > 0)
                {
                    where += " AND col.name = ANY(@colors) ";
                    parameters.Add(new NpgsqlParameter("@colors", colors));
                }

                if (sizes != null && sizes.Length > 0)
                {
                    where += " AND s.name = ANY(@sizes) ";
                    parameters.Add(new NpgsqlParameter("@sizes", sizes));
                }

                if (minPrice.HasValue)
                {
                    where += " AND COALESCE(p.discountprice, p.price) >= @minPrice ";
                    parameters.Add(new NpgsqlParameter("@minPrice", minPrice));
                }

                if (maxPrice.HasValue)
                {
                    where += " AND COALESCE(p.discountprice, p.price) <= @maxPrice ";
                    parameters.Add(new NpgsqlParameter("@maxPrice", maxPrice));
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    where += " AND LOWER(p.name) LIKE LOWER(@search) ";
                    parameters.Add(new NpgsqlParameter("@search", $"%{search}%"));
                }

                // ============================
                // 🔹 COUNT
                // ============================
                string countQuery = $@"
        SELECT COUNT(DISTINCT p.id)
        FROM products p
        LEFT JOIN product_colors pc ON p.id = pc.productid
        LEFT JOIN colors col ON pc.colorid = col.id
        LEFT JOIN product_sizes ps ON p.id = ps.product_id
        LEFT JOIN sizes s ON ps.size_id = s.id
        {where};";

                int totalRecords;

                using (var countCmd = new NpgsqlCommand(countQuery, conn))
                {
                    foreach (var p in parameters)
                        countCmd.Parameters.Add(new NpgsqlParameter(p.ParameterName, p.Value));

                    totalRecords = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
                }

                // ============================
                // 🔹 MAIN QUERY (NO IMAGE TABLE)
                // ============================
                string query = $@"
SELECT p.*, 
       c.name AS category_name,
       sc.name AS subcategory_name,
       b.name AS brand_name,

       COALESCE(
           ARRAY_AGG(DISTINCT col.name) 
           FILTER (WHERE col.id IS NOT NULL), '{{}}'
       ) AS colors,

       COALESCE(
           ARRAY_AGG(DISTINCT s.name) 
           FILTER (WHERE s.id IS NOT NULL), '{{}}'
       ) AS sizes

FROM products p

LEFT JOIN categories c ON p.categoryid = c.id
LEFT JOIN categories sc ON p.subcategoryid = sc.id
LEFT JOIN brands b ON p.brandid = b.id

LEFT JOIN product_colors pc ON p.id = pc.productid
LEFT JOIN colors col ON pc.colorid = col.id

LEFT JOIN product_sizes ps ON p.id = ps.product_id
LEFT JOIN sizes s ON ps.size_id = s.id

{where}

GROUP BY p.id, c.name, sc.name, b.name
ORDER BY p.createddate DESC
LIMIT @pageSize OFFSET @offset;
";

                var list = new List<object>();

                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    foreach (var p in parameters)
                        cmd.Parameters.Add(new NpgsqlParameter(p.ParameterName, p.Value));

                    cmd.Parameters.AddWithValue("@pageSize", basePageSize);
                    cmd.Parameters.AddWithValue("@offset", offset);

                    using var reader = await cmd.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        string mainImage = reader["MainImage"]?.ToString();

                        list.Add(new
                        {
                            Id = reader["Id"],
                            Name = reader["Name"]?.ToString(),
                            Slug = reader["Slug"]?.ToString(),

                            ShortDescription = reader["ShortDescription"]?.ToString(),
                            Description = reader["Description"]?.ToString(),

                            CategoryId = reader["CategoryId"],
                            SubCategoryId = reader["SubCategoryId"] == DBNull.Value ? null : reader["SubCategoryId"],
                            BrandId = reader["BrandId"] == DBNull.Value ? null : reader["BrandId"],

                            CategoryName = reader["category_name"]?.ToString(),
                            SubCategoryName = reader["subcategory_name"]?.ToString(),
                            BrandName = reader["brand_name"]?.ToString(),

                            Colors = reader["colors"] == DBNull.Value ? new string[] { } : (string[])reader["colors"],
                            Sizes = reader["sizes"] == DBNull.Value ? new string[] { } : (string[])reader["sizes"],

                            Price = reader["Price"],
                            DiscountPrice = reader["DiscountPrice"] == DBNull.Value ? null : reader["DiscountPrice"],
                            CostPrice = reader["CostPrice"] == DBNull.Value ? null : reader["CostPrice"],

                            TaxPercentage = reader["TaxPercentage"] == DBNull.Value ? null : reader["TaxPercentage"],

                            SKU = reader["SKU"]?.ToString(),

                            StockQuantity = reader["StockQuantity"],
                            MinStockQuantity = reader["MinStockQuantity"] == DBNull.Value ? null : reader["MinStockQuantity"],
                            TrackInventory = reader["TrackInventory"],

                            // ✅ IMAGE FIX
                            MainImage = mainImage,
                            GalleryImages = string.IsNullOrEmpty(mainImage)
                                ? new string[] { }
                                : new string[] { mainImage },

                            Weight = reader["Weight"] == DBNull.Value ? null : reader["Weight"],
                            Length = reader["Length"] == DBNull.Value ? null : reader["Length"],
                            Width = reader["Width"] == DBNull.Value ? null : reader["Width"],
                            Height = reader["Height"] == DBNull.Value ? null : reader["Height"],

                            MetaTitle = reader["MetaTitle"]?.ToString(),
                            MetaDescription = reader["MetaDescription"]?.ToString(),
                            MetaKeywords = reader["MetaKeywords"]?.ToString(),

                            IsActive = reader["IsActive"],
                            IsFeatured = reader["IsFeatured"],

                            CreatedDate = reader["CreatedDate"]
                        });
                    }
                }

                return new
                {
                    success = true,
                    page = basePage,
                    pageSize = basePageSize,
                    totalRecords,
                    totalPages = (int)Math.Ceiling((double)totalRecords / basePageSize),
                    data = list
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    success = false,
                    message = ex.Message
                };
            }
        }
        public async Task<object> GetProductById(Guid productId)
        {
            try
            {
                using (var conn = new NpgsqlConnection(DbConnection))
                {
                    await conn.OpenAsync();

                    string query = @"
SELECT p.*, 
       c.name AS category_name,
       sc.name AS subcategory_name,
       b.name AS brand_name,

       -- ✅ Colors
       COALESCE(
           ARRAY_AGG(DISTINCT col.name) 
           FILTER (WHERE col.id IS NOT NULL), '{}'
       ) AS colors,

       -- ✅ Sizes
       COALESCE(
           ARRAY_AGG(DISTINCT s.name) 
           FILTER (WHERE s.id IS NOT NULL), '{}'
       ) AS sizes

FROM products p

LEFT JOIN categories c ON p.categoryid = c.id
LEFT JOIN categories sc ON p.subcategoryid = sc.id
LEFT JOIN brands b ON p.brandid = b.id

LEFT JOIN product_colors pc ON p.id = pc.productid
LEFT JOIN colors col ON pc.colorid = col.id

LEFT JOIN product_sizes ps ON p.id = ps.product_id
LEFT JOIN sizes s ON ps.size_id = s.id

WHERE p.id = @productId AND p.isdeleted = FALSE

GROUP BY p.id, c.name, sc.name, b.name;
";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@productId", productId);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var product = new
                                {
                                    Id = reader.GetGuid(reader.GetOrdinal("Id")),
                                    Name = reader["Name"]?.ToString(),
                                    Slug = reader["Slug"]?.ToString(),
                                    ShortDescription = reader["ShortDescription"]?.ToString(),
                                    Description = reader["Description"]?.ToString(),

                                    CategoryId = reader["CategoryId"],
                                    SubCategoryId = reader["SubCategoryId"] == DBNull.Value ? null : reader["SubCategoryId"],
                                    BrandId = reader["BrandId"] == DBNull.Value ? null : reader["BrandId"],

                                    CategoryName = reader["category_name"] == DBNull.Value ? null : reader["category_name"].ToString(),
                                    SubCategoryName = reader["subcategory_name"] == DBNull.Value ? null : reader["subcategory_name"].ToString(),
                                    BrandName = reader["brand_name"] == DBNull.Value ? null : reader["brand_name"].ToString(),

                                    Colors = reader["colors"] == DBNull.Value
                                        ? new string[] { }
                                        : (string[])reader["colors"],

                                    Sizes = reader["sizes"] == DBNull.Value
                                        ? new string[] { }
                                        : (string[])reader["sizes"],

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
                                };

                                return new
                                {
                                    success = true,
                                    data = product
                                };
                            }
                        }
                    }

                    return new
                    {
                        success = false,
                        message = "Product not found"
                    };
                }
            }
            catch (Exception ex)
            {
                return new
                {
                    success = false,
                    message = "Error while fetching product",
                    error = ex.Message
                };
            }
        }

        public async Task<object> GetAllProductsOfAdmin(string userEmail, int? page, int? pageSize)
        {
            try
            {
                int basePage = (page.HasValue && page.Value > 0) ? page.Value : 1;
                int basePageSize = (pageSize.HasValue && pageSize.Value > 0) ? pageSize.Value : 100;

                using (var conn = new NpgsqlConnection(DbConnection))
                {
                    await conn.OpenAsync();

                    // ✅ STEP 1: Get UserId
                    string userQuery = @"SELECT ""Id"" FROM ""AspNetUsers"" WHERE ""Email"" = @Email LIMIT 1";

                    Guid userId;

                    using (var userCmd = new NpgsqlCommand(userQuery, conn))
                    {
                        if (string.IsNullOrEmpty(userEmail))
                        {
                            return new { success = false, message = "User email is missing" };
                        }

                        userCmd.Parameters.AddWithValue("@Email", userEmail);

                        var result = await userCmd.ExecuteScalarAsync();

                        if (result == null)
                        {
                            return new { success = false, message = "User not found" };
                        }

                        userId = Guid.Parse(result.ToString());
                    }

                    int offset = (basePage - 1) * basePageSize;

                    // ✅ STEP 2: Total count
                    string countQuery = @"
                SELECT COUNT(*)
                FROM products
                WHERE isdeleted = FALSE
                AND createdby = @UserId;";

                    int totalRecords;

                    using (var countCmd = new NpgsqlCommand(countQuery, conn))
                    {
                        countCmd.Parameters.AddWithValue("@UserId", userId);
                        totalRecords = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
                    }

                    // ✅ STEP 3: MAIN QUERY (JOIN + COLORS + SIZES)
                    string getQuery = @"
SELECT p.*, 
       c.name AS category_name,
       sc.name AS subcategory_name,
       b.name AS brand_name,

       -- ✅ Colors
       COALESCE(
           ARRAY_AGG(DISTINCT col.name) 
           FILTER (WHERE col.id IS NOT NULL), '{}'
       ) AS colors,

       -- ✅ Sizes
       COALESCE(
           ARRAY_AGG(DISTINCT s.name) 
           FILTER (WHERE s.id IS NOT NULL), '{}'
       ) AS sizes

FROM products p

LEFT JOIN categories c ON p.categoryid = c.id
LEFT JOIN categories sc ON p.subcategoryid = sc.id
LEFT JOIN brands b ON p.brandid = b.id

LEFT JOIN product_colors pc ON p.id = pc.productid
LEFT JOIN colors col ON pc.colorid = col.id

LEFT JOIN product_sizes ps ON p.id = ps.product_id
LEFT JOIN sizes s ON ps.size_id = s.id

WHERE p.isdeleted = FALSE
AND p.createdby = @UserId

GROUP BY p.id, c.name, sc.name, b.name

ORDER BY p.createddate DESC
LIMIT @pageSize OFFSET @offset;
";

                    var products = new List<object>();

                    using (var cmd = new NpgsqlCommand(getQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.Parameters.AddWithValue("@pageSize", basePageSize);
                        cmd.Parameters.AddWithValue("@offset", offset);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                products.Add(new
                                {
                                    Id = reader.GetGuid(reader.GetOrdinal("Id")),
                                    Name = reader["Name"]?.ToString(),
                                    Slug = reader["Slug"]?.ToString(),
                                    ShortDescription = reader["ShortDescription"]?.ToString(),
                                    Description = reader["Description"]?.ToString(),

                                    CategoryId = reader.GetGuid(reader.GetOrdinal("CategoryId")),
                                    SubCategoryId = reader["SubCategoryId"] == DBNull.Value ? null : (Guid?)reader["SubCategoryId"],
                                    BrandId = reader["BrandId"] == DBNull.Value ? null : (Guid?)reader["BrandId"],

                                    // ✅ Names
                                    CategoryName = reader["category_name"] == DBNull.Value ? null : reader["category_name"].ToString(),
                                    SubCategoryName = reader["subcategory_name"] == DBNull.Value ? null : reader["subcategory_name"].ToString(),
                                    BrandName = reader["brand_name"] == DBNull.Value ? null : reader["brand_name"].ToString(),

                                    // ✅ Colors & Sizes
                                    Colors = reader["colors"] == DBNull.Value
                                        ? new string[] { }
                                        : (string[])reader["colors"],

                                    Sizes = reader["sizes"] == DBNull.Value
                                        ? new string[] { }
                                        : (string[])reader["sizes"],

                                    Price = Convert.ToDecimal(reader["Price"]),
                                    DiscountPrice = reader["DiscountPrice"] == DBNull.Value ? null : (decimal?)reader["DiscountPrice"],
                                    CostPrice = reader["CostPrice"] == DBNull.Value ? null : (decimal?)reader["CostPrice"],
                                    TaxPercentage = reader["TaxPercentage"] == DBNull.Value ? null : (decimal?)reader["TaxPercentage"],

                                    SKU = reader["SKU"]?.ToString(),

                                    StockQuantity = Convert.ToInt32(reader["StockQuantity"]),
                                    MinStockQuantity = reader["MinStockQuantity"] == DBNull.Value ? null : (int?)reader["MinStockQuantity"],
                                    TrackInventory = Convert.ToBoolean(reader["TrackInventory"]),

                                    MainImage = reader["MainImage"]?.ToString(),

                                    GalleryImages = reader["GalleryImages"] == DBNull.Value
                                        ? new string[] { }
                                        : (string[])reader["GalleryImages"],

                                    Weight = reader["Weight"] == DBNull.Value ? null : (decimal?)reader["Weight"],
                                    Length = reader["Length"] == DBNull.Value ? null : (decimal?)reader["Length"],
                                    Width = reader["Width"] == DBNull.Value ? null : (decimal?)reader["Width"],
                                    Height = reader["Height"] == DBNull.Value ? null : (decimal?)reader["Height"],

                                    MetaTitle = reader["MetaTitle"]?.ToString(),
                                    MetaDescription = reader["MetaDescription"]?.ToString(),
                                    MetaKeywords = reader["MetaKeywords"]?.ToString(),

                                    IsActive = Convert.ToBoolean(reader["IsActive"]),
                                    IsFeatured = Convert.ToBoolean(reader["IsFeatured"]),
                                    IsDeleted = Convert.ToBoolean(reader["IsDeleted"]),

                                    CreatedBy = reader["CreatedBy"] == DBNull.Value ? null : reader["CreatedBy"].ToString(),
                                    CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
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
string baseSlug)
        {
            try
            {
                using (var conn = new NpgsqlConnection(DbConnection))
                {
                    await conn.OpenAsync();

                    // Check product exists
                    string checkQuery = @"SELECT id FROM products 
                                  WHERE id=@Id AND IsDeleted=FALSE LIMIT 1";

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

                    // Generate slug
                    string slug = baseSlug;

                    // Upload Main Image
                    string mainImagePath = null;

                    if (request.MainImage != null)
                    {
                        var folder = Path.Combine(
                            Directory.GetCurrentDirectory(),
                            "wwwroot/uploads/products");

                        if (!Directory.Exists(folder))
                            Directory.CreateDirectory(folder);

                        var fileName = Guid.NewGuid() +
                                       Path.GetExtension(request.MainImage.FileName);

                        var filePath = Path.Combine(folder, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await request.MainImage.CopyToAsync(stream);
                        }

                        mainImagePath = "/uploads/products/" + fileName;
                    }

                    // Upload Gallery Images
                    List<string> galleryPaths = new();

                    if (request.GalleryImages != null)
                    {
                        foreach (var image in request.GalleryImages)
                        {
                            var fileName = Guid.NewGuid() +
                                           Path.GetExtension(image.FileName);

                            var filePath = Path.Combine(
                                Directory.GetCurrentDirectory(),
                                "wwwroot/uploads/products",
                                fileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await image.CopyToAsync(stream);
                            }

                            galleryPaths.Add("/uploads/products/" + fileName);
                        }
                    }

                    // Update query
                    string updateQuery = @"
            UPDATE products SET
                Name=@Name,
                Slug=@Slug,
                ShortDescription=@ShortDescription,
                Description=@Description,

                CategoryId=@CategoryId,
                SubCategoryId=@SubCategoryId,
                BrandId=@BrandId,

                Price=@Price,
                DiscountPrice=@DiscountPrice,
                CostPrice=@CostPrice,
                TaxPercentage=@TaxPercentage,

                StockQuantity=@StockQuantity,
                MinStockQuantity=@MinStockQuantity,
                TrackInventory=@TrackInventory,

                MainImage=COALESCE(@MainImage, MainImage),
                GalleryImages=COALESCE(@GalleryImages, GalleryImages),

                Weight=@Weight,
                Length=@Length,
                Width=@Width,
                Height=@Height,

                MetaTitle=@MetaTitle,
                MetaDescription=@MetaDescription,
                MetaKeywords=@MetaKeywords,

                IsActive=@IsActive,
                IsFeatured=@IsFeatured,
                IsDeleted=@IsDeleted

            WHERE Id=@Id
            RETURNING Id;";

                    using (var cmd = new NpgsqlCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", productId);

                        cmd.Parameters.AddWithValue("@Name", request.Name);
                        cmd.Parameters.AddWithValue("@Slug", slug);

                        cmd.Parameters.AddWithValue("@ShortDescription",
                            (object?)request.ShortDescription ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@Description",
                            (object?)request.Description ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@CategoryId", request.CategoryId);

                        cmd.Parameters.AddWithValue("@SubCategoryId",
                            (object?)request.SubCategoryId ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@BrandId",
                            (object?)request.BrandId ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@Price", request.Price);

                        cmd.Parameters.AddWithValue("@DiscountPrice",
                            (object?)request.DiscountPrice ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@CostPrice",
                            (object?)request.CostPrice ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@TaxPercentage",
                            (object?)request.TaxPercentage ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@StockQuantity", request.StockQuantity);

                        cmd.Parameters.AddWithValue("@MinStockQuantity",
                            (object?)request.MinStockQuantity ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@TrackInventory", request.TrackInventory);

                        cmd.Parameters.AddWithValue("@MainImage",
                            (object?)mainImagePath ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@GalleryImages",
                            galleryPaths.Any() ? galleryPaths.ToArray() : DBNull.Value);

                        cmd.Parameters.AddWithValue("@Weight",
                            (object?)request.Weight ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@Length",
                            (object?)request.Length ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@Width",
                            (object?)request.Width ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@Height",
                            (object?)request.Height ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@MetaTitle",
                            (object?)request.MetaTitle ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@MetaDescription",
                            (object?)request.MetaDescription ?? DBNull.Value);

                        cmd.Parameters.AddWithValue("@MetaKeywords",
                            (object?)request.MetaKeywords ?? DBNull.Value);

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
                    message = ex.Message
                };
            }
        }

        public async Task<object> DeleteProduct(Guid productId)
        {
            try
            {
                using (var conn = new NpgsqlConnection(DbConnection))
                {
                    await conn.OpenAsync();

                    string query = @"
            UPDATE products
            SET IsDeleted = TRUE
            WHERE Id = @Id";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", productId);

                        int rowsAffected = await cmd.ExecuteNonQueryAsync();

                        if (rowsAffected == 0)
                        {
                            return new
                            {
                                success = false,
                                message = "Product not found"
                            };
                        }

                        return new
                        {
                            success = true,
                            message = "Product deleted successfully"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new
                {
                    success = false,
                    message = "Error while deleting product",
                    error = ex.Message
                };
            }
        }

        public async Task<object> RestoreProduct(Guid productId)
        {
            try
            {
                using (var conn = new NpgsqlConnection(DbConnection))
                {
                    await conn.OpenAsync();

                    string query = @"
            UPDATE products
            SET IsDeleted = FALSE
            WHERE Id = @Id";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", productId);

                        int rowsAffected = await cmd.ExecuteNonQueryAsync();

                        if (rowsAffected == 0)
                        {
                            return new
                            {
                                success = false,
                                message = "Product not found"
                            };
                        }

                        return new
                        {
                            success = true,
                            message = "Product restored successfully"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new
                {
                    success = false,
                    message = "Error while restoring product",
                    error = ex.Message
                };
            }
        }

        public async Task<object> PermanentDeleteProduct(Guid productId)
        {
            try
            {
                using (var conn = new NpgsqlConnection(DbConnection))
                {
                    await conn.OpenAsync();

                    string query = @"DELETE FROM products WHERE Id = @Id";

                    using (var cmd = new NpgsqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", productId);

                        int rowsAffected = await cmd.ExecuteNonQueryAsync();

                        if (rowsAffected == 0)
                        {
                            return new
                            {
                                success = false,
                                message = "Product not found"
                            };
                        }

                        return new
                        {
                            success = true,
                            message = "Product permanently deleted"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new
                {
                    success = false,
                    message = "Error while deleting product permanently",
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
