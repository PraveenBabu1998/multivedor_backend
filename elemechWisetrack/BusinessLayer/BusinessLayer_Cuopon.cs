using elemechWisetrack.Models;

namespace elemechWisetrack.BusinessLayer
{
    public interface IBusinessLayer_Cuopon
    {
        Task<object> CreateCoupon(string email, CouponCodeCreate request);
        Task<object> GetCoupons();
        Task<object> GetCouponById(Guid id);
        Task<object> UpdateCoupon(Guid id, CouponCodeCreate request);
        Task<object> DeleteCoupon(Guid id);
        Task<object> ApplyCoupon(string email, ApplyCouponRequest request);
        Task<object> GetCouponUsage();
    }

    public partial interface IBusinessLayer : IBusinessLayer_Cuopon { }

    public partial class BusinessLayer
    {
        public async Task<object> CreateCoupon(string email, CouponCodeCreate request)
            => await _dataBaseLayer.CreateCoupon(email, request);

        public async Task<object> GetCoupons()
            => await _dataBaseLayer.GetCoupons();

        public async Task<object> GetCouponById(Guid id)
            => await _dataBaseLayer.GetCouponById(id);

        public async Task<object> UpdateCoupon(Guid id, CouponCodeCreate request)
            => await _dataBaseLayer.UpdateCoupon(id, request);

        public async Task<object> DeleteCoupon(Guid id)
            => await _dataBaseLayer.DeleteCoupon(id);

        public async Task<object> ApplyCoupon(string email, ApplyCouponRequest request)
            => await _dataBaseLayer.ApplyCoupon(email, request);
        public async Task<object> GetCouponUsage()
        {
            return await _dataBaseLayer.GetCouponUsage();
        }
    }
}
