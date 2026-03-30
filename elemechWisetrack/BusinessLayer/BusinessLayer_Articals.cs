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
                var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");

                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                var fileName = Guid.NewGuid() + Path.GetExtension(model.Image.FileName);
                var fullPath = Path.Combine(folderPath, fileName);

                using var stream = new FileStream(fullPath, FileMode.Create);
                await model.Image.CopyToAsync(stream);

                imagePath = "/images/" + fileName;
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
            string imagePath = model.ImageUrl;

            if (model.Image != null)
            {
                var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");

                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                var fileName = Guid.NewGuid() + Path.GetExtension(model.Image.FileName);
                var fullPath = Path.Combine(folderPath, fileName);

                using var stream = new FileStream(fullPath, FileMode.Create);
                await model.Image.CopyToAsync(stream);

                imagePath = "/images/" + fileName;
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