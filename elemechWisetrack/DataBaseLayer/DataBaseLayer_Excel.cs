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

                string imageFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/products");

                if (!Directory.Exists(imageFolder))
                    Directory.CreateDirectory(imageFolder);

                // ================= GET USER =================

                string getUserQuery = @"SELECT ""Id"" FROM ""AspNetUsers"" 
                                WHERE ""Email""=@Email LIMIT 1";

                Guid userId;

                using (var userCmd = new NpgsqlCommand(getUserQuery, con))
                {
                    userCmd.Parameters.AddWithValue("@Email", userEmail);

                    var result = await userCmd.ExecuteScalarAsync();

                    if (result == null)
                        return new { success = false, message = "User not found" };

                    userId = Guid.Parse(result.ToString());
                }

                foreach (var item in products)
                {
                    string productName = item.E ?? "";
                    string categoryName = item.B ?? "";
                    string shortDescription = item.G ?? "";
                    string specification = item.H ?? "";
                    string productCode = item.M ?? "";
                    string description = item.U ?? "";

                    if (string.IsNullOrWhiteSpace(productName))
                        continue;

                    int.TryParse(item.I, out int stockQty);
                    decimal.TryParse(item.P, out decimal price);
                    decimal.TryParse(item.Q, out decimal sellingPrice);

                    bool isFeatured = item.Y?.ToLower() == "true" || item.Y == "1";
                    bool isActive = item.AA?.ToLower() == "true" || item.AA == "1";

                    string slug = productName.ToLower().Replace(" ", "-");

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
                            var imagePath = await DownloadImageAsync(url, imageFolder);

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
                            brandId = (Guid)result;
                    }

                    // ================= CATEGORY =================

                    Guid? categoryId = null;

                    if (!string.IsNullOrWhiteSpace(categoryName))
                    {
                        var parts = categoryName.Split('>');
                        string lastCategory = parts.Last().Trim();

                        string catQuery = "SELECT id FROM categories WHERE LOWER(name)=LOWER(@name) LIMIT 1";

                        using var cmd = new NpgsqlCommand(catQuery, con);
                        cmd.Parameters.AddWithValue("@name", lastCategory);

                        var result = await cmd.ExecuteScalarAsync();

                        if (result != null)
                            categoryId = (Guid)result;
                    }

                    // ================= PRODUCT ID =================

                    Guid productId = Guid.NewGuid();
                    string sku = "SKU-" + DateTime.Now.Ticks;

                    // ================= INSERT PRODUCT =================

                    string insertQuery = @"
            INSERT INTO products
            (
                id,name,slug,shortdescription,description,
                categoryid,brandid,price,discountprice,
                sku,stockquantity,mainimage,galleryimages,
                isfeatured,isactive,product_code,specification,
                createddate,createdby
            )
            VALUES
            (
                @id,@name,@slug,@shortdescription,@description,
                @categoryid,@brandid,@price,@discountprice,
                @sku,@stockquantity,@mainimage,@galleryimages,
                @isfeatured,@isactive,@productcode,@specification,
                @createddate,@createdby
            )";

                    using (var cmd = new NpgsqlCommand(insertQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@id", productId);
                        cmd.Parameters.AddWithValue("@name", productName);
                        cmd.Parameters.AddWithValue("@slug", slug);
                        cmd.Parameters.AddWithValue("@shortdescription", shortDescription ?? "");
                        cmd.Parameters.AddWithValue("@description", description ?? "");
                        cmd.Parameters.AddWithValue("@categoryid", (object?)categoryId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@brandid", (object?)brandId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@price", price);
                        cmd.Parameters.AddWithValue("@discountprice", sellingPrice);
                        cmd.Parameters.AddWithValue("@sku", sku);
                        cmd.Parameters.AddWithValue("@stockquantity", stockQty);
                        cmd.Parameters.AddWithValue("@mainimage", mainImage ?? "");
                        cmd.Parameters.AddWithValue("@galleryimages", galleryImages);
                        cmd.Parameters.AddWithValue("@isfeatured", isFeatured);
                        cmd.Parameters.AddWithValue("@isactive", isActive);
                        cmd.Parameters.AddWithValue("@productcode", productCode ?? "");
                        cmd.Parameters.AddWithValue("@specification", specification ?? "");
                        cmd.Parameters.AddWithValue("@createddate", DateTime.UtcNow);
                        cmd.Parameters.AddWithValue("@createdby", userId);

                        await cmd.ExecuteNonQueryAsync();
                    }

                    // ================= EXTRACT COLORS =================

                    var colors = new HashSet<string>();

                    if (shortDescription.Contains("Colour:") || shortDescription.Contains("Color:"))
                    {
                        string part = shortDescription.Contains("Colour:")
                            ? shortDescription.Split("Colour:")[1]
                            : shortDescription.Split("Color:")[1];

                        part = part.Split(",")[0].Trim();

                        foreach (var c in part.Split('&'))
                        {
                            if (!string.IsNullOrWhiteSpace(c))
                                colors.Add(c.Trim());
                        }
                    }

                    foreach (var color in colors)
                    {
                        string query = "SELECT id FROM colors WHERE LOWER(name)=LOWER(@name) LIMIT 1";

                        using var colorCmd = new NpgsqlCommand(query, con);
                        colorCmd.Parameters.AddWithValue("@name", color);

                        var colorId = await colorCmd.ExecuteScalarAsync();

                        if (colorId != null)
                        {
                            string insert = "INSERT INTO product_colors(ProductId,ColorId) VALUES(@pid,@cid)";

                            using var cmd = new NpgsqlCommand(insert, con);
                            cmd.Parameters.AddWithValue("@pid", productId);
                            cmd.Parameters.AddWithValue("@cid", (Guid)colorId);

                            await cmd.ExecuteNonQueryAsync();
                        }
                    }

                    // ================= EXTRACT SIZES =================

                    var sizes = new HashSet<string>();

                    if (shortDescription.Contains("Size:"))
                    {
                        string part = shortDescription.Split("Size:")[1];
                        part = part.Split(",")[0].Trim();

                        foreach (var s in part.Split('&'))
                        {
                            if (!string.IsNullOrWhiteSpace(s))
                                sizes.Add(s.Trim());
                        }
                    }

                    foreach (var size in sizes)
                    {
                        string query = "SELECT id FROM sizes WHERE LOWER(name)=LOWER(@name) LIMIT 1";

                        using var sizeCmd = new NpgsqlCommand(query, con);
                        sizeCmd.Parameters.AddWithValue("@name", size);

                        var sizeId = await sizeCmd.ExecuteScalarAsync();

                        if (sizeId != null)
                        {
                            string insert = "INSERT INTO product_sizes(product_id,size_id) VALUES(@pid,@sid)";

                            using var cmd = new NpgsqlCommand(insert, con);
                            cmd.Parameters.AddWithValue("@pid", productId);
                            cmd.Parameters.AddWithValue("@sid", (Guid)sizeId);

                            await cmd.ExecuteNonQueryAsync();
                        }
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

        public async Task<string> DownloadImageAsync(string imageUrl, string folderPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(imageUrl))
                    return "";

                var response = await httpClient.GetAsync(imageUrl);

                if (!response.IsSuccessStatusCode)
                    return "";

                var bytes = await response.Content.ReadAsByteArrayAsync();

                string extension = Path.GetExtension(imageUrl);

                if (string.IsNullOrEmpty(extension))
                    extension = ".jpg";

                string fileName = Guid.NewGuid().ToString() + extension;

                string fullPath = Path.Combine(folderPath, fileName);

                await File.WriteAllBytesAsync(fullPath, bytes);

                // return relative path to store in DB
                return "/uploads/products/" + fileName;
            }
            catch
            {
                return "";
            }
        }

    }
}
