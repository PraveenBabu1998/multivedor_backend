using CareerCracker.S3Services;
using elemechWisetrack.Models;
using System.Text.RegularExpressions;

namespace elemechWisetrack.BusinessLayer
{
    public interface IBusinessLayer_Articals
    {
        Task<object> AddArtical(string email, ArticalModel model);
        Task<object> GetAllArticals();
        Task<object> GetArticalById(Guid id);
        Task<object> UpdateArtical(string email, Guid id, ArticalModel model);
        Task<object> DeleteArtical(string email, Guid id);
    }

    public partial interface IBusinessLayer : IBusinessLayer_Articals { }

    public partial class BusinessLayer
    {
        public async Task<object> AddArtical(string email, ArticalModel model)
        {
            string imagePath = "";

            if (model.Image != null)
            {
                var uploaded = await S3StorageHelper.UploadFileAsync(model.Image, "uploads/articles");
                imagePath = uploaded ?? "";
            }

            model.ImageUrl = imagePath;
            model.Slug = SlugHelper.GenerateSlug(model.Title);

            return await _dataBaseLayer.AddArtical(email, model);
        }

        public async Task<object> GetAllArticals()
        {
            return await _dataBaseLayer.GetAllArticals();
        }

        public async Task<object> GetArticalById(Guid id)
        {
            return await _dataBaseLayer.GetArticalById(id);
        }

        public async Task<object> UpdateArtical(string email, Guid id, ArticalModel model)
        {
            string imagePath = model.ImageUrl ?? "";

            if (model.Image != null)
            {
                await S3StorageHelper.DeleteStoredMediaAsync(model.ImageUrl);
                var uploaded = await S3StorageHelper.UploadFileAsync(model.Image, "uploads/articles");
                imagePath = uploaded ?? "";
            }

            model.ImageUrl = imagePath;
            model.Slug = SlugHelper.GenerateSlug(model.Title);

            return await _dataBaseLayer.UpdateArtical(email, id, model);
        }

        public async Task<object> DeleteArtical(string email, Guid id)
        {
            return await _dataBaseLayer.DeleteArtical(email, id);
        }

        public static class SlugHelper
        {
            public static string GenerateSlug(string title)
            {
                if (string.IsNullOrEmpty(title)) return "";

                string slug = title.ToLower().Trim();

                slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
                slug = Regex.Replace(slug, @"\s+", "-");
                slug = Regex.Replace(slug, @"-+", "-");

                return $"{slug}-{Guid.NewGuid().ToString()[..6]}";
            }
        }
    }
}