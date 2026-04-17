using CareerCracker.S3Services;
using elemechWisetrack.Models;
using Npgsql;

namespace elemechWisetrack.DataBaseLayer
{
    public interface IDataBaseLayer_Excel
    {
        Task<object> InsertExcelFileCategory(List<string> dataCellB);
        Task<object> InsertExcelFileBrands(List<string> dataCellG);
        Task<object> InsertExcelFileColors(List<string> dataCellG);
        Task<object> InsertExcelFileSize(List<string> dataCellG);
        Task<object> InsertExcelFileProducts(List<ExcelProductRow> products, string userEmail);
    }

    public partial interface IDataBaseLayer : IDataBaseLayer_Excel { }

    public partial class DataBaseLayer
    {
        public async Task<object> InsertExcelFileCategory(List<string> dataCellB)
        {
            using (var con = new NpgsqlConnection(DbConnection))
            {
                await con.OpenAsync();

                foreach (var value in dataCellB)
                {
                    var parts = value.Split('>');

                    string category = parts.Length > 0 ? parts[0].Trim() : null;
                    string subCategory = parts.Length > 1 ? parts[1].Trim() : null;
                    string childCategory = parts.Length > 2 ? parts[2].Trim() : null;

                    Guid categoryId;
                    Guid subCategoryId;

                    // =============================
                    // 1️⃣ CATEGORY
                    // =============================
                    string getCategory = @"SELECT id FROM categories 
                                   WHERE name = @name AND parentid IS NULL";

                    using (var cmd = new NpgsqlCommand(getCategory, con))
                    {
                        cmd.Parameters.AddWithValue("@name", category);

                        var result = await cmd.ExecuteScalarAsync();

                        if (result == null)
                        {
                            categoryId = Guid.NewGuid();

                            string insertCategory = @"INSERT INTO categories
                        (id,name,slug,parentid)
                        VALUES(@id,@name,@slug,NULL)";

                            using (var insertCmd = new NpgsqlCommand(insertCategory, con))
                            {
                                insertCmd.Parameters.AddWithValue("@id", categoryId);
                                insertCmd.Parameters.AddWithValue("@name", category);
                                insertCmd.Parameters.AddWithValue("@slug", category.ToLower().Replace(" ", "-"));

                                await insertCmd.ExecuteNonQueryAsync();
                            }
                        }
                        else
                        {
                            categoryId = (Guid)result;
                        }
                    }

                    // =============================
                    // 2️⃣ SUBCATEGORY
                    // =============================
                    if (subCategory != null)
                    {
                        string getSubCategory = @"SELECT id FROM categories
                                          WHERE name=@name AND parentid=@parentid";

                        using (var cmd = new NpgsqlCommand(getSubCategory, con))
                        {
                            cmd.Parameters.AddWithValue("@name", subCategory);
                            cmd.Parameters.AddWithValue("@parentid", categoryId);

                            var result = await cmd.ExecuteScalarAsync();

                            if (result == null)
                            {
                                subCategoryId = Guid.NewGuid();

                                string insertSubCategory = @"INSERT INTO categories
                            (id,name,slug,parentid)
                            VALUES(@id,@name,@slug,@parentid)";

                                using (var insertCmd = new NpgsqlCommand(insertSubCategory, con))
                                {
                                    insertCmd.Parameters.AddWithValue("@id", subCategoryId);
                                    insertCmd.Parameters.AddWithValue("@name", subCategory);
                                    insertCmd.Parameters.AddWithValue("@slug", subCategory.ToLower().Replace(" ", "-"));
                                    insertCmd.Parameters.AddWithValue("@parentid", categoryId);

                                    await insertCmd.ExecuteNonQueryAsync();
                                }
                            }
                            else
                            {
                                subCategoryId = (Guid)result;
                            }
                        }

                        // =============================
                        // 3️⃣ CHILD CATEGORY
                        // =============================
                        if (childCategory != null)
                        {
                            string checkChild = @"SELECT id FROM categories
                                          WHERE name=@name AND parentid=@parentid";

                            using (var cmd = new NpgsqlCommand(checkChild, con))
                            {
                                cmd.Parameters.AddWithValue("@name", childCategory);
                                cmd.Parameters.AddWithValue("@parentid", subCategoryId);

                                var result = await cmd.ExecuteScalarAsync();

                                if (result == null)
                                {
                                    string insertChild = @"INSERT INTO categories
                                (id,name,slug,parentid)
                                VALUES(@id,@name,@slug,@parentid)";

                                    using (var insertCmd = new NpgsqlCommand(insertChild, con))
                                    {
                                        insertCmd.Parameters.AddWithValue("@id", Guid.NewGuid());
                                        insertCmd.Parameters.AddWithValue("@name", childCategory);
                                        insertCmd.Parameters.AddWithValue("@slug", childCategory.ToLower().Replace(" ", "-"));
                                        insertCmd.Parameters.AddWithValue("@parentid", subCategoryId);

                                        await insertCmd.ExecuteNonQueryAsync();
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return new
            {
                success = true,
                message = "Excel categories imported successfully"
            };
        }

        public async Task<object> InsertExcelFileBrands(List<string> dataCellG)
        {
            using (var con = new NpgsqlConnection(DbConnection))
            {
                await con.OpenAsync();

                foreach (var value in dataCellG)
                {
                    if (string.IsNullOrWhiteSpace(value))
                        continue;

                    string brandName = "";

                    if (value.Contains("Brand:"))
                    {
                        var brandPart = value.Split("Brand:")[1];

                        brandName = brandPart.Split(",")[0].Trim();
                    }

                    if (string.IsNullOrWhiteSpace(brandName))
                        continue;

                    // Check if brand already exists
                    string checkQuery = @"SELECT id FROM brands WHERE LOWER(name) = LOWER(@name)";

                    using (var checkCmd = new NpgsqlCommand(checkQuery, con))
                    {
                        checkCmd.Parameters.AddWithValue("@name", brandName);

                        var result = await checkCmd.ExecuteScalarAsync();

                        if (result == null)
                        {
                            string slug = brandName.ToLower().Replace(" ", "-");

                            string insertQuery = @"INSERT INTO brands 
                        (id,name,slug,createdby)
                        VALUES(@id,@name,@slug,@createdby)";

                            using (var insertCmd = new NpgsqlCommand(insertQuery, con))
                            {
                                insertCmd.Parameters.AddWithValue("@id", Guid.NewGuid());
                                insertCmd.Parameters.AddWithValue("@name", brandName);
                                insertCmd.Parameters.AddWithValue("@slug", slug);
                                insertCmd.Parameters.AddWithValue("@createdby", Guid.Parse("00000000-0000-0000-0000-000000000000")); // replace with userId

                                await insertCmd.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }
            }

            return new
            {
                success = true,
                message = "Brands inserted successfully"
            };
        }

        public async Task<object> InsertExcelFileColors(List<string> dataCellG)
        {
            var colors = new HashSet<string>();

            foreach (var row in dataCellG)
            {
                if (string.IsNullOrWhiteSpace(row))
                    continue;

                if (row.Contains("Colour:") || row.Contains("Color:"))
                {
                    string colorPart = "";

                    if (row.Contains("Colour:"))
                        colorPart = row.Split("Colour:")[1];
                    else
                        colorPart = row.Split("Color:")[1];

                    colorPart = colorPart.Split(",")[0].Trim();

                    var splitColors = colorPart.Split('&');

                    foreach (var color in splitColors)
                    {
                        var c = color.Trim();

                        if (!string.IsNullOrWhiteSpace(c))
                            colors.Add(c);
                    }
                }
            }

            using (var con = new NpgsqlConnection(DbConnection))
            {
                await con.OpenAsync();

                foreach (var color in colors)
                {
                    string checkQuery = "SELECT id FROM colors WHERE LOWER(name)=LOWER(@name)";

                    using (var checkCmd = new NpgsqlCommand(checkQuery, con))
                    {
                        checkCmd.Parameters.AddWithValue("@name", color);

                        var exist = await checkCmd.ExecuteScalarAsync();

                        if (exist == null)
                        {
                            string slug = color.ToLower().Replace(" ", "-");

                            string insertQuery = @"INSERT INTO colors
                    (id,name,slug)
                    VALUES(@id,@name,@slug)";

                            using (var cmd = new NpgsqlCommand(insertQuery, con))
                            {
                                cmd.Parameters.AddWithValue("@id", Guid.NewGuid());
                                cmd.Parameters.AddWithValue("@name", color);
                                cmd.Parameters.AddWithValue("@slug", slug);

                                await cmd.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }
            }

            return new
            {
                success = true,
                message = "Colors inserted successfully",
                total = colors.Count
            };
        }

        public async Task<object> InsertExcelFileSize(List<string> dataCellG)
        {
            var sizes = new HashSet<string>();

            foreach (var row in dataCellG)
            {
                if (string.IsNullOrWhiteSpace(row))
                    continue;

                if (row.Contains("Size:"))
                {
                    var sizePart = row.Split("Size:")[1];
                    var size = sizePart.Split(",")[0].Trim();

                    if (!string.IsNullOrWhiteSpace(size))
                        sizes.Add(size);
                }
            }

            using (var con = new NpgsqlConnection(DbConnection))
            {
                await con.OpenAsync();

                foreach (var size in sizes)
                {
                    string checkQuery = "SELECT id FROM sizes WHERE LOWER(name)=LOWER(@name)";

                    using (var checkCmd = new NpgsqlCommand(checkQuery, con))
                    {
                        checkCmd.Parameters.AddWithValue("@name", size);

                        var exist = await checkCmd.ExecuteScalarAsync();

                        if (exist == null)
                        {
                            string slug = size.ToLower().Replace(" ", "-");

                            string insertQuery = @"INSERT INTO sizes
                    (id,name,slug)
                    VALUES(@id,@name,@slug)";

                            using (var cmd = new NpgsqlCommand(insertQuery, con))
                            {
                                cmd.Parameters.AddWithValue("@id", Guid.NewGuid());
                                cmd.Parameters.AddWithValue("@name", size);
                                cmd.Parameters.AddWithValue("@slug", slug);

                                await cmd.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }
            }

            return new
            {
                success = true,
                message = "Sizes inserted successfully",
                total = sizes.Count
            };
        }

        public async Task<object> InsertExcelFileProducts(List<ExcelProductRow> products, string userEmail)
        {
            using (var con = new NpgsqlConnection(DbConnection))
            {
                await con.OpenAsync();

                int insertedCount = 0;
                int skippedCount = 0;

                // ================= GET USER =================

                string getUserQuery = @"SELECT ""Id"" FROM ""AspNetUsers"" 
                                WHERE LOWER(""Email"")=LOWER(@Email) LIMIT 1";

                Guid userId;

                using (var userCmd = new NpgsqlCommand(getUserQuery, con))
                {
                    userCmd.Parameters.AddWithValue("@Email", userEmail);

                    var result = await userCmd.ExecuteScalarAsync();

                    if (result == null || !Guid.TryParse(result.ToString(), out userId))
                        return new { success = false, message = "User not found" };
                }

                foreach (var item in products)
                {
                    string productName = item.E ?? "";
                    string categoryName = item.B ?? "";
                    string shortDescription = item.G ?? "";
                    string description = item.U ?? "";

                    if (string.IsNullOrWhiteSpace(productName))
                        continue;

                    int.TryParse(item.I, out int stockQty);
                    decimal.TryParse(item.P, out decimal price);
                    decimal.TryParse(item.Q, out decimal sellingPrice);

                    bool isFeatured = item.Y?.ToLower() == "true" || item.Y == "1";
                    bool isActive = item.AA?.ToLower() == "true" || item.AA == "1";

                    string slug = productName.Trim().ToLowerInvariant().Replace(" ", "-");

                    // ================= DUPLICATE CHECK =================

                    string checkQuery = "SELECT COUNT(*) FROM products WHERE slug=@slug";

                    using (var checkCmd = new NpgsqlCommand(checkQuery, con))
                    {
                        checkCmd.Parameters.AddWithValue("@slug", slug);

                        var exists = (long)await checkCmd.ExecuteScalarAsync();

                        if (exists > 0)
                        {
                            skippedCount++;
                            continue;
                        }
                    }

                    // ================= DOWNLOAD IMAGES =================

                    string mainImage = "";
                    string[] galleryImages = Array.Empty<string>();

                    if (!string.IsNullOrWhiteSpace(item.T))
                    {
                        var urls = item.T.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                         .Select(x => x.Trim())
                                         .ToList();

                        List<string> savedImages = new List<string>();

                        foreach (var url in urls)
                        {
                            var imagePath = await DownloadImageAsync(url);

                            if (!string.IsNullOrEmpty(imagePath))
                                savedImages.Add(imagePath);
                        }

                        if (savedImages.Count > 0)
                        {
                            mainImage = savedImages[0];

                            if (savedImages.Count > 1)
                                galleryImages = savedImages.Skip(1).ToArray();
                        }
                    }

                    // ================= BRAND =================

                    string brandName = "";

                    if (shortDescription.Contains("Brand:"))
                    {
                        var brandPart = shortDescription.Split("Brand:")[1];
                        brandName = brandPart.Split(",")[0].Trim();
                    }

                    Guid? brandId = null;

                    if (!string.IsNullOrWhiteSpace(brandName))
                    {
                        string brandQuery = "SELECT id FROM brands WHERE LOWER(name)=LOWER(@name) LIMIT 1";

                        using var cmd = new NpgsqlCommand(brandQuery, con);
                        cmd.Parameters.AddWithValue("@name", brandName);

                        var result = await cmd.ExecuteScalarAsync();

                        if (result != null)
                        {
                            brandId = (Guid)result;
                        }
                        else
                        {
                            brandId = Guid.NewGuid();
                            string brandSlug = brandName.Trim().ToLowerInvariant().Replace(" ", "-");
                            string insertBrand = @"INSERT INTO brands (id,name,slug,description,logo,isactive,createdby,createddate)
                                                   VALUES (@id,@name,@slug,@description,@logo,@isactive,@createdby,@createddate)";
                            using var insertBrandCmd = new NpgsqlCommand(insertBrand, con);
                            insertBrandCmd.Parameters.AddWithValue("@id", brandId.Value);
                            insertBrandCmd.Parameters.AddWithValue("@name", brandName);
                            insertBrandCmd.Parameters.AddWithValue("@slug", brandSlug);
                            insertBrandCmd.Parameters.AddWithValue("@description", DBNull.Value);
                            insertBrandCmd.Parameters.AddWithValue("@logo", DBNull.Value);
                            insertBrandCmd.Parameters.AddWithValue("@isactive", true);
                            insertBrandCmd.Parameters.AddWithValue("@createdby", userId);
                            insertBrandCmd.Parameters.AddWithValue("@createddate", DateTime.UtcNow);
                            await insertBrandCmd.ExecuteNonQueryAsync();
                        }
                    }

                    // ================= CATEGORY =================

                    Guid? categoryId = null;

                    if (!string.IsNullOrWhiteSpace(categoryName))
                    {
                        var parts = categoryName.Split('>');
                        Guid? parentCategoryId = null;

                        foreach (var part in parts.Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)))
                        {
                            string catQuery;
                            using var cmd = new NpgsqlCommand();
                            cmd.Connection = con;
                            cmd.Parameters.AddWithValue("@name", part);

                            if (parentCategoryId.HasValue)
                            {
                                catQuery = @"SELECT id FROM categories
                                             WHERE LOWER(name)=LOWER(@name)
                                             AND parentid = @parentid
                                             LIMIT 1";
                                cmd.Parameters.AddWithValue("@parentid", parentCategoryId.Value);
                            }
                            else
                            {
                                catQuery = @"SELECT id FROM categories
                                             WHERE LOWER(name)=LOWER(@name)
                                             AND parentid IS NULL
                                             LIMIT 1";
                            }

                            cmd.CommandText = catQuery;

                            var result = await cmd.ExecuteScalarAsync();

                            if (result != null)
                            {
                                parentCategoryId = (Guid)result;
                            }
                            else
                            {
                                Guid newCategoryId = Guid.NewGuid();
                                string categorySlug = part.Trim().ToLowerInvariant().Replace(" ", "-");
                                string insertCategory = @"INSERT INTO categories (id,name,slug,parentid,image,isactive)
                                                          VALUES (@id,@name,@slug,@parentid,@image,@isactive)";
                                using var insertCategoryCmd = new NpgsqlCommand(insertCategory, con);
                                insertCategoryCmd.Parameters.AddWithValue("@id", newCategoryId);
                                insertCategoryCmd.Parameters.AddWithValue("@name", part);
                                insertCategoryCmd.Parameters.AddWithValue("@slug", categorySlug);
                                insertCategoryCmd.Parameters.AddWithValue("@parentid", (object?)parentCategoryId ?? DBNull.Value);
                                insertCategoryCmd.Parameters.AddWithValue("@image", DBNull.Value);
                                insertCategoryCmd.Parameters.AddWithValue("@isactive", true);
                                await insertCategoryCmd.ExecuteNonQueryAsync();
                                parentCategoryId = newCategoryId;
                            }
                        }

                        categoryId = parentCategoryId;
                    }

                    // ================= COLOR =================

                    Guid? colorId = null;
                    if (shortDescription.Contains("Colour:") || shortDescription.Contains("Color:"))
                    {
                        string colorPart = shortDescription.Contains("Colour:")
                            ? shortDescription.Split("Colour:")[1]
                            : shortDescription.Split("Color:")[1];

                        string firstColor = colorPart.Split(",")[0]
                                                     .Split('&', StringSplitOptions.RemoveEmptyEntries)
                                                     .FirstOrDefault()?.Trim() ?? "";

                        if (!string.IsNullOrWhiteSpace(firstColor))
                        {
                            string colorQuery = "SELECT id FROM colors WHERE LOWER(name)=LOWER(@name) LIMIT 1";
                            using var colorCmd = new NpgsqlCommand(colorQuery, con);
                            colorCmd.Parameters.AddWithValue("@name", firstColor);
                            var result = await colorCmd.ExecuteScalarAsync();
                            if (result != null)
                            {
                                colorId = (Guid)result;
                            }
                            else
                            {
                                colorId = Guid.NewGuid();
                                string colorSlug = firstColor.Trim().ToLowerInvariant().Replace(" ", "-");
                                string insertColor = @"INSERT INTO colors (id,name,slug,hexcode,isactive,isdeleted,createddate)
                                                       VALUES (@id,@name,@slug,@hexcode,@isactive,@isdeleted,@createddate)";
                                using var insertColorCmd = new NpgsqlCommand(insertColor, con);
                                insertColorCmd.Parameters.AddWithValue("@id", colorId.Value);
                                insertColorCmd.Parameters.AddWithValue("@name", firstColor);
                                insertColorCmd.Parameters.AddWithValue("@slug", colorSlug);
                                insertColorCmd.Parameters.AddWithValue("@hexcode", DBNull.Value);
                                insertColorCmd.Parameters.AddWithValue("@isactive", true);
                                insertColorCmd.Parameters.AddWithValue("@isdeleted", false);
                                insertColorCmd.Parameters.AddWithValue("@createddate", DateTime.UtcNow);
                                await insertColorCmd.ExecuteNonQueryAsync();
                            }
                        }
                    }

                    // ================= SIZE =================

                    Guid? sizeId = null;
                    if (shortDescription.Contains("Size:"))
                    {
                        string sizePart = shortDescription.Split("Size:")[1];
                        string firstSize = sizePart.Split(",")[0]
                                                   .Split('&', StringSplitOptions.RemoveEmptyEntries)
                                                   .FirstOrDefault()?.Trim() ?? "";

                        if (!string.IsNullOrWhiteSpace(firstSize))
                        {
                            string sizeQuery = "SELECT id FROM sizes WHERE LOWER(name)=LOWER(@name) LIMIT 1";
                            using var sizeCmd = new NpgsqlCommand(sizeQuery, con);
                            sizeCmd.Parameters.AddWithValue("@name", firstSize);
                            var result = await sizeCmd.ExecuteScalarAsync();
                            if (result != null)
                            {
                                sizeId = (Guid)result;
                            }
                            else
                            {
                                sizeId = Guid.NewGuid();
                                string sizeSlug = firstSize.Trim().ToLowerInvariant().Replace(" ", "-");
                                string insertSize = @"INSERT INTO sizes (id,name,slug,isactive,isdeleted,createddate,updateddate)
                                                      VALUES (@id,@name,@slug,@isactive,@isdeleted,@createddate,@updateddate)";
                                using var insertSizeCmd = new NpgsqlCommand(insertSize, con);
                                insertSizeCmd.Parameters.AddWithValue("@id", sizeId.Value);
                                insertSizeCmd.Parameters.AddWithValue("@name", firstSize);
                                insertSizeCmd.Parameters.AddWithValue("@slug", sizeSlug);
                                insertSizeCmd.Parameters.AddWithValue("@isactive", true);
                                insertSizeCmd.Parameters.AddWithValue("@isdeleted", false);
                                insertSizeCmd.Parameters.AddWithValue("@createddate", DateTime.UtcNow);
                                insertSizeCmd.Parameters.AddWithValue("@updateddate", DBNull.Value);
                                await insertSizeCmd.ExecuteNonQueryAsync();
                            }
                        }
                    }

                    if (!categoryId.HasValue)
                    {
                        skippedCount++;
                        continue;
                    }

                    // ================= PRODUCT ID =================

                    Guid productId = Guid.NewGuid();
                    string sku = !string.IsNullOrWhiteSpace(item.M) ? item.M.Trim() : "SKU-" + DateTime.UtcNow.Ticks;

                    // ================= INSERT PRODUCT =================

                    string insertQuery = @"
            INSERT INTO products
            (
                id,name,slug,shortdescription,description,
                categoryid,brandid,colorid,sizeid,price,discountprice,
                sku,stockquantity,mainimage,galleryimages,
                isfeatured,isactive,isdeleted,createddate,createdby
            )
            VALUES
            (
                @id,@name,@slug,@shortdescription,@description,
                @categoryid,@brandid,@colorid,@sizeid,@price,@discountprice,
                @sku,@stockquantity,@mainimage,@galleryimages,
                @isfeatured,@isactive,@isdeleted,@createddate,@createdby
            )";

                    using (var cmd = new NpgsqlCommand(insertQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@id", productId);
                        cmd.Parameters.AddWithValue("@name", productName);
                        cmd.Parameters.AddWithValue("@slug", slug);
                        cmd.Parameters.AddWithValue("@shortdescription", shortDescription ?? "");
                        cmd.Parameters.AddWithValue("@description", description ?? "");
                        cmd.Parameters.AddWithValue("@categoryid", categoryId.Value);
                        cmd.Parameters.AddWithValue("@brandid", (object?)brandId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@colorid", (object?)colorId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@sizeid", (object?)sizeId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@price", price);
                        cmd.Parameters.AddWithValue("@discountprice", sellingPrice);
                        cmd.Parameters.AddWithValue("@sku", sku);
                        cmd.Parameters.AddWithValue("@stockquantity", stockQty);
                        cmd.Parameters.AddWithValue("@mainimage", mainImage ?? "");
                        cmd.Parameters.AddWithValue("@galleryimages", galleryImages);
                        cmd.Parameters.AddWithValue("@isfeatured", isFeatured);
                        cmd.Parameters.AddWithValue("@isactive", isActive);
                        cmd.Parameters.AddWithValue("@isdeleted", false);
                        cmd.Parameters.AddWithValue("@createddate", DateTime.UtcNow);
                        cmd.Parameters.AddWithValue("@createdby", userId);

                        await cmd.ExecuteNonQueryAsync();
                    }

                    insertedCount++;
                }

                return new
                {
                    success = true,
                    inserted = insertedCount,
                    skipped = skippedCount
                };
            }
        }

        private static readonly HttpClient httpClient = new HttpClient();

        public async Task<string> DownloadImageAsync(string imageUrl)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(imageUrl))
                    return "";

                var response = await httpClient.GetAsync(imageUrl);

                if (!response.IsSuccessStatusCode)
                    return "";

                var bytes = await response.Content.ReadAsByteArrayAsync();

                string extension = Path.GetExtension(imageUrl.Split('?')[0]);

                if (string.IsNullOrEmpty(extension))
                    extension = ".jpg";

                var contentType = response.Content.Headers.ContentType?.MediaType;
                if (string.IsNullOrWhiteSpace(contentType))
                    contentType = "image/jpeg";

                var uploaded = await S3StorageHelper.UploadBytesAsync(bytes, extension, contentType, "uploads/products");
                return uploaded ?? "";
            }
            catch
            {
                return "";
            }
        }

    }
}
