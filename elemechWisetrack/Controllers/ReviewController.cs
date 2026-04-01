using elemechWisetrack.BusinessLayer;
using elemechWisetrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[ApiController]
[Route("api/review")]
[Authorize]
public class ReviewController : ControllerBase
{
    private readonly IBusinessLayer _businessLayer;

    public ReviewController(IBusinessLayer businessLayer)
    {
        _businessLayer = businessLayer;
    }

    // ✅ ADD REVIEW
    [HttpPost("add/{productId}")]
    public async Task<IActionResult> AddReview(string productId, [FromForm] ReviewModel request)
    {
        string email = User.FindFirst(ClaimTypes.Email)?.Value;

        var result = await _businessLayer.AddReviews(productId, email, request);

        return Ok(result);
    }

    // ✅ GET REVIEWS BY PRODUCT
    [HttpGet("product/{productId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetReviews(string productId)
    {
        var result = await _businessLayer.GetReviewsByProduct(productId);
        return Ok(result);
    }

    // ✅ UPDATE REVIEW
    [HttpPut("update/{reviewId}")]
    public async Task<IActionResult> UpdateReview(string reviewId, [FromForm] ReviewModel request)
    {
        string email = User.FindFirst(ClaimTypes.Email)?.Value;

        var result = await _businessLayer.UpdateReview(reviewId, email, request);

        return Ok(result);
    }

    // ✅ DELETE REVIEW
    [HttpDelete("delete/{reviewId}")]
    public async Task<IActionResult> DeleteReview(string reviewId)
    {
        string email = User.FindFirst(ClaimTypes.Email)?.Value;

        var result = await _businessLayer.DeleteReview(reviewId, email);

        return Ok(result);
    }
}