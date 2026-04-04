using elemechWisetrack.Models;
using elemechWisetrack.others;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace elemechWisetrack.BusinessLayer
{
    public interface IBusinessLayer_Products
    {
        Task<object> AddSingleProduct(string userEmail, ProductInsertModel request);
        Task<object> GetAllProducts(
    int? page, int? pageSize,
    Guid[]? categoryIds,
    Guid? subCategoryId,
    Guid[]? brandIds,
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
    ProductInsertModel request);

        Task<object> DeleteProduct(Guid productId);
        Task<object> RestoreProduct(Guid productId);
        Task<object> PermanentDeleteProduct(Guid productId);
        Task<object> UploadProductsExcel(IFormFile file,string userEmail);
        Task<object> ToggleSalesStatus(Guid productId);
        Task<object> GetSalesProducts(
    int? page, int? pageSize,
    Guid[]? categoryIds,
    Guid? subCategoryId,
    Guid[]? brandIds,
    string[]? colors,
    string[]? sizes,
    decimal? minPrice,
    decimal? maxPrice,
    string? search
);
    }

    public partial interface IBusinessLayer : IBusinessLayer_Products
    {

    }

    public partial class BusinessLayer
    {
        public async Task<object> AddSingleProduct(string userEmail, ProductInsertModel request)
        {
            if (string.IsNullOrEmpty(request.Name))
                return "Product Name is required";

            if (request.CategoryId == Guid.Empty)
                return "Category id is required";

            if (request.BrandId == Guid.Empty)
                return "Brand is required";

            if (request.Price <= 0)
                return "Price is required";

            string baseSlug = GenerateSlugProduct(request.Name);

            return await _dataBaseLayer.AddSingleProduct(userEmail, request, baseSlug);
        }


        public async Task<object> GetAllProducts(
    int? page, int? pageSize,
    Guid[]? categoryIds,
    Guid? subCategoryId,
    Guid[]? brandIds,
    string[]? colors,
    string[]? sizes,
    decimal? minPrice,
    decimal? maxPrice,
    string? search)
        {
            return await _dataBaseLayer.GetAllProducts(
                page, pageSize,
                categoryIds, subCategoryId, brandIds,
                colors, sizes,
                minPrice, maxPrice,
                search
            );
        }

        public async Task<object> GetProductById(Guid productId)
        {
            return await _dataBaseLayer.GetProductById(productId); // ✅ MUST await
        }

        public async Task<object> GetAllProductsOfAdmin(string userEmail, int? page, int? pageSize)
        {
            return await _dataBaseLayer.GetAllProductsOfAdmin(userEmail,page, pageSize);
        }

        public async Task<object> UpdateProduct(
    Guid productId,
    string userEmail,
    ProductInsertModel request)
        {
            
            // Generate slug
            string baseSlug = GenerateSlugProduct(request.Name);

            return await _dataBaseLayer.UpdateProduct(
                productId,
                userEmail,
                request,
                baseSlug);
        }

        public async Task<object> DeleteProduct(Guid productId)
        {
            return await _dataBaseLayer.DeleteProduct(productId);
        }

        public async Task<object> RestoreProduct(Guid productId)
        {
            return await _dataBaseLayer.RestoreProduct(productId);
        }

        public async Task<object> PermanentDeleteProduct(Guid productId)
        {

            return await _dataBaseLayer.PermanentDeleteProduct(productId);
        }

        public async Task<object> UploadProductsExcel(IFormFile file, string userEmail)
        {
            var dataCellB = ExcelReader.ReadColumnB(file);
            var dataCellG = ExcelReader.ReadColumnG(file);
            //var dataCellV = ExcelReader.ReadColumnV(file);
            var readExceldata = ExcelReader.ReadExcelData(file);

            var insertCategoryCellB = await _dataBaseLayer.InsertExcelFileCategory(dataCellB);
            var insetBarndsCellG = await _dataBaseLayer.InsertExcelFileBrands(dataCellG);
            var insertColorCellG = await _dataBaseLayer.InsertExcelFileColors(dataCellG);
            var insertSizeCellG = await _dataBaseLayer.InsertExcelFileSize(dataCellG);
            var insertProductsData = await _dataBaseLayer.InsertExcelFileProducts(readExceldata, userEmail);

            return insertProductsData;
        }

        public string GenerateSlugProduct(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            // Convert to lowercase
            string slug = name.ToLower();

            // Remove special characters
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");

            // Replace multiple spaces with single space
            slug = Regex.Replace(slug, @"\s+", " ").Trim();

            // Replace spaces with hyphen
            slug = slug.Replace(" ", "-");

            return slug;
        }

        public async Task<object> ToggleSalesStatus(Guid productId)
        {
            return await _dataBaseLayer.ToggleSalesStatus(productId);
        }
        public async Task<object> GetSalesProducts(
    int? page, int? pageSize,
    Guid[]? categoryIds,
    Guid? subCategoryId,
    Guid[]? brandIds,
    string[]? colors,
    string[]? sizes,
    decimal? minPrice,
    decimal? maxPrice,
    string? search)
        {
            return await _dataBaseLayer.GetSalesProducts(
                page, pageSize,
                categoryIds, subCategoryId, brandIds,
                colors, sizes,
                minPrice, maxPrice,
                search
            );
        }
    }
}
