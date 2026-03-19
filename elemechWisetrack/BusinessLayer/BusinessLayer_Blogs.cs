using elemechWisetrack.Models;

namespace elemechWisetrack.BusinessLayer
{
    public interface IBusinessLayer_Blogs
    {
        Task<object> AddBlog(AddBlogDto model, string userId);
        Task<object> GetBlogs();
        Task<object> GetBlogById(string id);
        Task<object> UpdateBlog(string id, AddBlogDto model, string userId);
        Task<object> DeleteBlog(string id, string userId);
    }

    public partial interface IBusinessLayer : IBusinessLayer_Blogs { }

    public partial class BusinessLayer
    {
        public async Task<object> AddBlog(AddBlogDto model, string userId)
        => await _dataBaseLayer.AddBlog(model, userId);

        public async Task<object> GetBlogs()
            => await _dataBaseLayer.GetBlogs();

        public async Task<object> GetBlogById(string id)
            => await _dataBaseLayer.GetBlogById(id);

        public async Task<object> UpdateBlog(string id, AddBlogDto model, string userId)
            => await _dataBaseLayer.UpdateBlog(id, model, userId);

        public async Task<object> DeleteBlog(string id, string userId)
            => await _dataBaseLayer.DeleteBlog(id, userId);
    }
}
