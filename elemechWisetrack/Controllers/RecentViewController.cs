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
    [AllowAnonymous]
    public async Task<IActionResult> AddRecent(string productId)
    {
        string email = User.FindFirst(ClaimTypes.Email)?.Value;

        // ✅ GET IP ADDRESS
        string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var result = await _businessLayer.AddRecentView(productId, email, ipAddress);

        return Ok(result);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetRecent()
    {
        string email = User.FindFirst(ClaimTypes.Email)?.Value;

        string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var result = await _businessLayer.GetRecentViews(email, ipAddress);

        return Ok(result);
    }
}