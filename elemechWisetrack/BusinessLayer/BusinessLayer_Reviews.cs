using elemechWisetrack.Models;
using Microsoft.AspNetCore.Mvc;

namespace elemechWisetrack.BusinessLayer
{
    public interface IBusinessLayer_Reviews
    {
        Task<object> AddReviews(string productId, string email, ReviewModel request);
        Task<object> GetReviewsByProduct(string productId);
        Task<object> UpdateReview(string reviewId, string email, ReviewModel request);
        Task<object> DeleteReview(string reviewId, string email);
    }

    public partial interface IBusinessLayer : IBusinessLayer_Reviews { }

    public partial class BusinessLayer
    {
        public async Task<object> AddReviews(string productId, string email, ReviewModel request)
        {
            return await _dataBaseLayer.AddReviews(productId, email, request);
        }

        public async Task<object> GetReviewsByProduct(string productId)
        {
            return await _dataBaseLayer.GetReviewsByProduct(productId);
        }

        public async Task<object> UpdateReview(string reviewId, string email, ReviewModel request)
        {
            return await _dataBaseLayer.UpdateReview(reviewId, email, request);
        }

        public async Task<object> DeleteReview(string reviewId, string email)
        {
            return await _dataBaseLayer.DeleteReview(reviewId, email);
        }
    }
}
