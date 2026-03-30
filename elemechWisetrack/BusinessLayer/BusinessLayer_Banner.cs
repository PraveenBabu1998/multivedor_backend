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
            string imagePath = "";

            if (model.Image != null)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(model.Image.FileName);
                var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                var filePath = Path.Combine(folder, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await model.Image.CopyToAsync(stream);

                imagePath = "/uploads/" + fileName;
            }

            return await _dataBaseLayer.CreateBanner(new CreateBannerDbModel
            {
                Title = model.Title,
                Image = imagePath,
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
                var fileName = Guid.NewGuid() + Path.GetExtension(model.Image.FileName);
                var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                var filePath = Path.Combine(folder, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await model.Image.CopyToAsync(stream);

                imagePath = "/uploads/" + fileName;
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