using CareerCracker.S3Services;
using elemechWisetrack.Models;
using Npgsql;
using OfficeOpenXml;

namespace elemechWisetrack.DataBaseLayer
{
    public interface IDataBaseLayer_Products
    {
        Task<object> AddProduct(string userEmail, ProductInsertModel request, string slug);
        Task<object> GetProducts();
        Task<object> GetSoftDeletedProducts();
        Task<object> UpdateProduct(Guid id, ProductInsertModel request, string slug);
        Task<object> SoftDeleteProduct(Guid id);
        Task<object> DeleteProduct(Guid id);
        Task<byte[]> ExportProductsExcel();
    }

    public partial interface IDataBaseLayer : IDataBaseLayer_Products { }

    public partial class DataBaseLayer
    {
        public async Task<object> AddProduct(string userEmail, ProductInsertModel request, string slug)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            // Validate required foreign key references before insert to avoid FK exceptions.
            string categoryCheckQuery = @"SELECT 1 FROM categories WHERE id = @Id LIMIT 1";
            using (var categoryCmd = new NpgsqlCommand(categoryCheckQuery, conn))
            {
                categoryCmd.Parameters.AddWithValue("@Id", request.CategoryId);
                var exists = await categoryCmd.ExecuteScalarAsync();
                if (exists == null)
                {
                    return new { success = false, message = "Invalid categoryid. Category not found." };
                }
            }

            if (request.SubCategoryId.HasValue)
            {
                using var subCategoryCmd = new NpgsqlCommand(categoryCheckQuery, conn);
                subCategoryCmd.Parameters.AddWithValue("@Id", request.SubCategoryId.Value);
                var exists = await subCategoryCmd.ExecuteScalarAsync();
                if (exists == null)
                {
                    return new { success = false, message = "Invalid subcategoryid. Subcategory not found." };
                }
            }

            async Task<object?> ValidateOptionalFk(string tableName, Guid? id, string fieldName)
            {
                if (!id.HasValue) return null;
                string query = $"SELECT 1 FROM {tableName} WHERE id = @Id LIMIT 1";
                using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id.Value);
                var exists = await cmd.ExecuteScalarAsync();
                if (exists == null)
                {
                    return new { success = false, message = $"Invalid {fieldName}. Record not found." };
                }
                return null;
            }

            var brandValidation = await ValidateOptionalFk("brands", request.BrandId, "brandid");
            if (brandValidation != null) return brandValidation;

            var colorValidation = await ValidateOptionalFk("colors", request.ColorId, "colorid");
            if (colorValidation != null) return colorValidation;

            var sizeValidation = await ValidateOptionalFk("sizes", request.SizeId, "sizeid");
            if (sizeValidation != null) return sizeValidation;

            string userQuery = @"SELECT ""Id"" FROM ""AspNetUsers"" WHERE LOWER(""Email"")=LOWER(@Email) LIMIT 1";
            Guid? userId = null;

            using (var cmd = new NpgsqlCommand(userQuery, conn))
            {
                cmd.Parameters.AddWithValue("@Email", userEmail ?? string.Empty);
                var userResult = await cmd.ExecuteScalarAsync();
                if (userResult != null && Guid.TryParse(userResult.ToString(), out var parsedUserId))
                {
                    userId = parsedUserId;
                }
            }

            string? mainImagePath = null;
            if (request.MainImage != null)
            {
                mainImagePath = await S3StorageHelper.UploadFileAsync(request.MainImage, "products");
            }

            var galleryImagePaths = new List<string>();
            if (request.GalleryImages != null && request.GalleryImages.Count > 0)
            {
                foreach (var image in request.GalleryImages)
                {
                    if (image == null || image.Length == 0)
                    {
                        continue;
                    }

                    var url = await S3StorageHelper.UploadFileAsync(image, "products");
                    if (!string.IsNullOrEmpty(url))
                        galleryImagePaths.Add(url);
                }
            }

            string insertQuery = @"
                INSERT INTO products
                (id, name, slug, shortdescription, description, categoryid, subcategoryid, brandid, colorid, sizeid,
                 price, discountprice, costprice, taxpercentage, sku, stockquantity, minstockquantity, trackinventory,
                 mainimage, galleryimages, weight, length, width, height, metatitle, metadescription, metakeywords,
                 isactive, isfeatured, isdeleted, createdby, createddate)
                VALUES
                (@Id, @Name, @Slug, @ShortDescription, @Description, @CategoryId, @SubCategoryId, @BrandId, @ColorId, @SizeId,
                 @Price, @DiscountPrice, @CostPrice, @TaxPercentage, @SKU, @StockQuantity, @MinStockQuantity, @TrackInventory,
                 @MainImage, @GalleryImages, @Weight, @Length, @Width, @Height, @MetaTitle, @MetaDescription, @MetaKeywords,
                 @IsActive, @IsFeatured, @IsDeleted, @CreatedBy, @CreatedDate)";

            Guid productId = Guid.NewGuid();

            using var insertCmd = new NpgsqlCommand(insertQuery, conn);
            insertCmd.Parameters.AddWithValue("@Id", productId);
            insertCmd.Parameters.AddWithValue("@Name", request.Name);
            insertCmd.Parameters.AddWithValue("@Slug", slug);
            insertCmd.Parameters.AddWithValue("@ShortDescription", (object?)request.ShortDescription ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@CategoryId", request.CategoryId);
            insertCmd.Parameters.AddWithValue("@SubCategoryId", (object?)request.SubCategoryId ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@BrandId", (object?)request.BrandId ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@ColorId", (object?)request.ColorId ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@SizeId", (object?)request.SizeId ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@Price", request.Price);
            insertCmd.Parameters.AddWithValue("@DiscountPrice", (object?)request.DiscountPrice ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@CostPrice", (object?)request.CostPrice ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@TaxPercentage", (object?)request.TaxPercentage ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@SKU", request.SKU);
            insertCmd.Parameters.AddWithValue("@StockQuantity", request.StockQuantity);
            insertCmd.Parameters.AddWithValue("@MinStockQuantity", (object?)request.MinStockQuantity ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@TrackInventory", request.TrackInventory);
            insertCmd.Parameters.AddWithValue("@MainImage", (object?)mainImagePath ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@GalleryImages", galleryImagePaths.Count > 0 ? galleryImagePaths.ToArray() : Array.Empty<string>());
            insertCmd.Parameters.AddWithValue("@Weight", (object?)request.Weight ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@Length", (object?)request.Length ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@Width", (object?)request.Width ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@Height", (object?)request.Height ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@MetaTitle", (object?)request.MetaTitle ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@MetaDescription", (object?)request.MetaDescription ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@MetaKeywords", (object?)request.MetaKeywords ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@IsActive", request.IsActive);
            insertCmd.Parameters.AddWithValue("@IsFeatured", request.IsFeatured);
            insertCmd.Parameters.AddWithValue("@IsDeleted", request.IsDeleted);
            insertCmd.Parameters.AddWithValue("@CreatedBy", (object?)userId ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@CreatedDate", DateTime.UtcNow);

            await insertCmd.ExecuteNonQueryAsync();

            return new { success = true, message = "Product inserted successfully", id = productId };
        }

        public async Task<object> GetProducts()
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            string query = @"
            SELECT p.id, p.name, p.slug, p.shortdescription, p.description, p.categoryid, p.subcategoryid, p.brandid,
                   p.colorid, p.sizeid, p.price, p.discountprice, p.costprice, p.taxpercentage, p.sku, p.stockquantity,
                   p.minstockquantity, p.trackinventory, p.mainimage, p.galleryimages, p.weight, p.length, p.width, p.height,
                   p.metatitle, p.metadescription, p.metakeywords, p.isactive, p.isfeatured, p.isdeleted, p.createdby, p.createddate,
                   COALESCE(NULLIF(TRIM(COALESCE(u.""FirstName"", '') || ' ' || COALESCE(u.""LastName"", '')), ''), u.""UserName"", u.""Email"", '') AS createdbyname,
                   b.name AS brandname, cat.name AS categoryname,
                   c.name AS colorname, c.hexcode AS colorhexcode,
                   s.name AS sizename
            FROM products p
            LEFT JOIN ""AspNetUsers"" u ON u.""Id"" = p.createdby::text
            LEFT JOIN brands b ON b.id = p.brandid
            LEFT JOIN categories cat ON cat.id = p.categoryid
            LEFT JOIN colors c ON c.id = p.colorid
            LEFT JOIN sizes s ON s.id = p.sizeid
            WHERE p.isdeleted = false
            ORDER BY p.createddate DESC";

            using var cmd = new NpgsqlCommand(query, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            var products = new List<object>();
            while (await reader.ReadAsync())
            {
                products.Add(new
                {
                    id = reader["id"],
                    name = reader["name"],
                    slug = reader["slug"],
                    shortdescription = reader["shortdescription"],
                    description = reader["description"],
                    categoryid = reader["categoryid"],
                    subcategoryid = reader["subcategoryid"],
                    brandid = reader["brandid"],
                    colorid = reader["colorid"],
                    sizeid = reader["sizeid"],
                    brandname = reader["brandname"],
                    categoryname = reader["categoryname"],
                    colorname = reader["colorname"],
                    colorhexcode = reader["colorhexcode"],
                    sizename = reader["sizename"],
                    price = reader["price"],
                    discountprice = reader["discountprice"],
                    costprice = reader["costprice"],
                    taxpercentage = reader["taxpercentage"],
                    sku = reader["sku"],
                    stockquantity = reader["stockquantity"],
                    minstockquantity = reader["minstockquantity"],
                    trackinventory = reader["trackinventory"],
                    mainimage = reader["mainimage"],
                    galleryimages = reader["galleryimages"],
                    weight = reader["weight"],
                    length = reader["length"],
                    width = reader["width"],
                    height = reader["height"],
                    metatitle = reader["metatitle"],
                    metadescription = reader["metadescription"],
                    metakeywords = reader["metakeywords"],
                    isactive = reader["isactive"],
                    isfeatured = reader["isfeatured"],
                    isdeleted = reader["isdeleted"],
                    createdby = reader["createdby"],
                    createdtime = reader["createddate"],
                    createdbyname = reader["createdbyname"]
                });
            }

            return new { success = true, data = products };
        }

        public async Task<object> GetSoftDeletedProducts()
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            string query = @"
            SELECT p.id, p.name, p.slug, p.shortdescription, p.description, p.categoryid, p.subcategoryid, p.brandid,
                   p.colorid, p.sizeid, p.price, p.discountprice, p.costprice, p.taxpercentage, p.sku, p.stockquantity,
                   p.minstockquantity, p.trackinventory, p.mainimage, p.galleryimages, p.weight, p.length, p.width, p.height,
                   p.metatitle, p.metadescription, p.metakeywords, p.isactive, p.isfeatured, p.isdeleted, p.createdby, p.createddate,
                   COALESCE(NULLIF(TRIM(COALESCE(u.""FirstName"", '') || ' ' || COALESCE(u.""LastName"", '')), ''), u.""UserName"", u.""Email"", '') AS createdbyname,
                   b.name AS brandname, cat.name AS categoryname,
                   c.name AS colorname, c.hexcode AS colorhexcode,
                   s.name AS sizename
            FROM products p
            LEFT JOIN ""AspNetUsers"" u ON u.""Id"" = p.createdby::text
            LEFT JOIN brands b ON b.id = p.brandid
            LEFT JOIN categories cat ON cat.id = p.categoryid
            LEFT JOIN colors c ON c.id = p.colorid
            LEFT JOIN sizes s ON s.id = p.sizeid
            WHERE p.isdeleted = true
            ORDER BY p.createddate DESC";

            using var cmd = new NpgsqlCommand(query, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            var products = new List<object>();
            while (await reader.ReadAsync())
            {
                products.Add(new
                {
                    id = reader["id"],
                    name = reader["name"],
                    slug = reader["slug"],
                    shortdescription = reader["shortdescription"],
                    description = reader["description"],
                    categoryid = reader["categoryid"],
                    subcategoryid = reader["subcategoryid"],
                    brandid = reader["brandid"],
                    colorid = reader["colorid"],
                    sizeid = reader["sizeid"],
                    brandname = reader["brandname"],
                    categoryname = reader["categoryname"],
                    colorname = reader["colorname"],
                    colorhexcode = reader["colorhexcode"],
                    sizename = reader["sizename"],
                    price = reader["price"],
                    discountprice = reader["discountprice"],
                    costprice = reader["costprice"],
                    taxpercentage = reader["taxpercentage"],
                    sku = reader["sku"],
                    stockquantity = reader["stockquantity"],
                    minstockquantity = reader["minstockquantity"],
                    trackinventory = reader["trackinventory"],
                    mainimage = reader["mainimage"],
                    galleryimages = reader["galleryimages"],
                    weight = reader["weight"],
                    length = reader["length"],
                    width = reader["width"],
                    height = reader["height"],
                    metatitle = reader["metatitle"],
                    metadescription = reader["metadescription"],
                    metakeywords = reader["metakeywords"],
                    isactive = reader["isactive"],
                    isfeatured = reader["isfeatured"],
                    isdeleted = reader["isdeleted"],
                    createdby = reader["createdby"],
                    createdtime = reader["createddate"],
                    createdbyname = reader["createdbyname"]
                });
            }

            return new { success = true, data = products };
        }

        public async Task<object> UpdateProduct(Guid id, ProductInsertModel request, string slug)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            string query = @"
UPDATE products
SET name = @Name,
    slug = @Slug,
    shortdescription = @ShortDescription,
    description = @Description,
    categoryid = @CategoryId,
    subcategoryid = @SubCategoryId,
    brandid = @BrandId,
    colorid = @ColorId,
    sizeid = @SizeId,
    price = @Price,
    discountprice = @DiscountPrice,
    costprice = @CostPrice,
    taxpercentage = @TaxPercentage,
    sku = @SKU,
    stockquantity = @StockQuantity,
    minstockquantity = @MinStockQuantity,
    trackinventory = @TrackInventory,
    weight = @Weight,
    length = @Length,
    width = @Width,
    height = @Height,
    metatitle = @MetaTitle,
    metadescription = @MetaDescription,
    metakeywords = @MetaKeywords,
    isactive = @IsActive,
    isfeatured = @IsFeatured
WHERE id = @Id";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.Parameters.AddWithValue("@Name", request.Name);
            cmd.Parameters.AddWithValue("@Slug", slug);
            cmd.Parameters.AddWithValue("@ShortDescription", (object?)request.ShortDescription ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CategoryId", request.CategoryId);
            cmd.Parameters.AddWithValue("@SubCategoryId", (object?)request.SubCategoryId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@BrandId", (object?)request.BrandId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ColorId", (object?)request.ColorId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@SizeId", (object?)request.SizeId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Price", request.Price);
            cmd.Parameters.AddWithValue("@DiscountPrice", (object?)request.DiscountPrice ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CostPrice", (object?)request.CostPrice ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@TaxPercentage", (object?)request.TaxPercentage ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@SKU", request.SKU);
            cmd.Parameters.AddWithValue("@StockQuantity", request.StockQuantity);
            cmd.Parameters.AddWithValue("@MinStockQuantity", (object?)request.MinStockQuantity ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@TrackInventory", request.TrackInventory);
            cmd.Parameters.AddWithValue("@Weight", (object?)request.Weight ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Length", (object?)request.Length ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Width", (object?)request.Width ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Height", (object?)request.Height ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@MetaTitle", (object?)request.MetaTitle ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@MetaDescription", (object?)request.MetaDescription ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@MetaKeywords", (object?)request.MetaKeywords ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IsActive", request.IsActive);
            cmd.Parameters.AddWithValue("@IsFeatured", request.IsFeatured);

            int rows = await cmd.ExecuteNonQueryAsync();
            if (rows == 0)
            {
                return new { success = false, message = "Product not found" };
            }

            return new { success = true, message = "Product updated successfully" };
        }

        public async Task<object> SoftDeleteProduct(Guid id)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            string query = @"UPDATE products SET isdeleted = true WHERE id = @Id";
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Id", id);

            int rows = await cmd.ExecuteNonQueryAsync();
            if (rows == 0)
            {
                return new { success = false, message = "Product not found" };
            }

            return new { success = true, message = "Product soft deleted successfully" };
        }

        public async Task<object> DeleteProduct(Guid id)
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            string query = @"DELETE FROM products WHERE id = @Id RETURNING id";
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Id", id);

            var deletedId = await cmd.ExecuteScalarAsync();
            if (deletedId == null)
            {
                return new { success = false, message = "Product not found" };
            }

            return new { success = true, message = "Product deleted permanently", id = deletedId };
        }

        public async Task<byte[]> ExportProductsExcel()
        {
            using var conn = new NpgsqlConnection(DbConnection);
            await conn.OpenAsync();

            string query = @"
            SELECT p.name, cat.name AS categoryname, b.name AS brandname, c.name AS colorname, c.hexcode AS colorhexcode,
                   s.name AS sizename, p.shortdescription, p.description, p.price, p.discountprice, p.sku,
                   p.stockquantity, p.isactive, p.isfeatured, p.createddate
            FROM products p
            LEFT JOIN categories cat ON cat.id = p.categoryid
            LEFT JOIN brands b ON b.id = p.brandid
            LEFT JOIN colors c ON c.id = p.colorid
            LEFT JOIN sizes s ON s.id = p.sizeid
            WHERE p.isdeleted = false
            ORDER BY p.createddate DESC";

            using var cmd = new NpgsqlCommand(query, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            ExcelPackage.License.SetNonCommercialPersonal("Cursor");
            using var package = new ExcelPackage();
            var sheet = package.Workbook.Worksheets.Add("Products");

            sheet.Cells[1, 1].Value = "Product Name";
            sheet.Cells[1, 2].Value = "Category";
            sheet.Cells[1, 3].Value = "Brand";
            sheet.Cells[1, 4].Value = "Color";
            sheet.Cells[1, 5].Value = "Color Hexcode";
            sheet.Cells[1, 6].Value = "Size";
            sheet.Cells[1, 7].Value = "Short Description";
            sheet.Cells[1, 8].Value = "Description";
            sheet.Cells[1, 9].Value = "Price";
            sheet.Cells[1, 10].Value = "Discount Price";
            sheet.Cells[1, 11].Value = "SKU";
            sheet.Cells[1, 12].Value = "Stock Quantity";
            sheet.Cells[1, 13].Value = "Is Active";
            sheet.Cells[1, 14].Value = "Is Featured";
            sheet.Cells[1, 15].Value = "Created Time";

            int row = 2;
            while (await reader.ReadAsync())
            {
                sheet.Cells[row, 1].Value = reader["name"]?.ToString();
                sheet.Cells[row, 2].Value = reader["categoryname"]?.ToString();
                sheet.Cells[row, 3].Value = reader["brandname"]?.ToString();
                sheet.Cells[row, 4].Value = reader["colorname"]?.ToString();
                sheet.Cells[row, 5].Value = reader["colorhexcode"]?.ToString();
                sheet.Cells[row, 6].Value = reader["sizename"]?.ToString();
                sheet.Cells[row, 7].Value = reader["shortdescription"]?.ToString();
                sheet.Cells[row, 8].Value = reader["description"]?.ToString();
                sheet.Cells[row, 9].Value = reader["price"];
                sheet.Cells[row, 10].Value = reader["discountprice"] is DBNull ? null : reader["discountprice"];
                sheet.Cells[row, 11].Value = reader["sku"]?.ToString();
                sheet.Cells[row, 12].Value = reader["stockquantity"];
                sheet.Cells[row, 13].Value = reader["isactive"];
                sheet.Cells[row, 14].Value = reader["isfeatured"];
                sheet.Cells[row, 15].Value = reader["createddate"];
                row++;
            }

            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
            return package.GetAsByteArray();
        }
    }
}
