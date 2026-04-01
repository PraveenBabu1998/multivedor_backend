using elemechWisetrack.BusinessLayer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[ApiController]
[Route("api/recent")]
[Authorize]
public class RecentViewController : ControllerBase
{
    private readonly IBusinessLayer _businessLayer;

    public RecentViewController(IBusinessLayer businessLayer)
    {
        _businessLayer = businessLayer;
    }

    // ✅ ADD RECENT VIEW
    [HttpPost("add/{productId}")]
    public async Task<IActionResult> AddRecent(string productId)
    {
        string email = User.FindFirst(ClaimTypes.Email)?.Value;

        var result = await _businessLayer.AddRecentView(productId, email);

        return Ok(result);
    }

    // ✅ GET RECENT PRODUCTS
    [HttpGet]
    public async Task<IActionResult> GetRecent()
    {
        string email = User.FindFirst(ClaimTypes.Email)?.Value;

        var result = await _businessLayer.GetRecentViews(email);

        return Ok(result);
    }
}