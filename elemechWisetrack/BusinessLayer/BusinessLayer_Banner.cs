using CareerCracker.S3Services;
using elemechWisetrack.DataBaseLayer;
using elemechWisetrack.Models;

namespace elemechWisetrack.BusinessLayer
{
    public interface IBusinessLayer_Banner
    {
        Task<object> CreateBanner(CreateBannerModel model);
        Task<object> GetBanners();
        Task<object> GetBannerById(Guid id);
        Task<object> UpdateBanner(UpdateBannerModel model);
        Task<object> DeleteBanner(Guid id);
    }

    public partial interface IBusinessLayer : IBusinessLayer_Banner { }

    public partial class BusinessLayer : IBusinessLayer
    {
        

        public async Task<object> CreateBanner(CreateBannerModel model)
        {
            string? imagePath = null;

            if (model.Image != null)
            {
                imagePath = await S3StorageHelper.UploadFileAsync(model.Image, "uploads/banners");
            }

            return await _dataBaseLayer.CreateBanner(new CreateBannerDbModel
            {
                Title = model.Title,
                Image = imagePath ?? "",
                Link = model.Link
            });
        }

        public async Task<object> GetBanners() => await _dataBaseLayer.GetBanners();

        public async Task<object> GetBannerById(Guid id) => await _dataBaseLayer.GetBannerById(id);

        public async Task<object> UpdateBanner(UpdateBannerModel model)
        {
            string imagePath = "";

            if (model.Image != null)
            {
                var existing = await _dataBaseLayer.GetBannerById(model.Id) as BannerModel;
                if (!string.IsNullOrEmpty(existing?.Image))
                    await S3StorageHelper.DeleteStoredMediaAsync(existing.Image);

                var uploaded = await S3StorageHelper.UploadFileAsync(model.Image, "uploads/banners");
                imagePath = uploaded ?? "";
            }

            return await _dataBaseLayer.UpdateBanner(new UpdateBannerDbModel
            {
                Id = model.Id,
                Title = model.Title,
                Image = imagePath,
                Link = model.Link,
                IsActive = model.IsActive
            });
        }

        public async Task<object> DeleteBanner(Guid id)
            => await _dataBaseLayer.DeleteBanner(id);
    }
}