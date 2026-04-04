namespace elemechWisetrack.BusinessLayer
{
    public interface IBusinessLayer_Recent
    {
        Task<object> AddRecentView(string productId, string email, string ipAddress);
        Task<object> GetRecentViews(string email, string ipAddress);
    }

    public partial interface IBusinessLayer : IBusinessLayer_Recent { }

    public partial class BusinessLayer
    {
        public async Task<object> AddRecentView(string productId, string email, string ipAddress)
        {
            return await _dataBaseLayer.AddRecentView(productId, email, ipAddress);
        }

        public async Task<object> GetRecentViews(string email, string ipAddress)
        {
            return await _dataBaseLayer.GetRecentViews(email, ipAddress);
        }
    }
}
