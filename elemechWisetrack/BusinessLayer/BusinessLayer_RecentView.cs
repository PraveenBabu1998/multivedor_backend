namespace elemechWisetrack.BusinessLayer
{
    public interface IBusinessLayer_Recent
    {
        Task<object> AddRecentView(string productId, string email);
        Task<object> GetRecentViews(string email);
    }

    public partial interface IBusinessLayer : IBusinessLayer_Recent { }

    public partial class BusinessLayer
    {
        public async Task<object> AddRecentView(string productId, string email)
        {
            return await _dataBaseLayer.AddRecentView(productId, email);
        }

        public async Task<object> GetRecentViews(string email)
        {
            return await _dataBaseLayer.GetRecentViews(email);
        }
    }
}
