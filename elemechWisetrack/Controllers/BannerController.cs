using elemechWisetrack.BusinessLayer;
using elemechWisetrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/banner")]
[Authorize]
public class BannerController : ControllerBase
{
    private readonly IBusinessLayer _businessLayer;

    public BannerController(IBusinessLayer businessLayer)
    {
        _businessLayer = businessLayer;
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateBanner([FromForm] CreateBannerModel model)
    {
        // remove link validation issue
        ModelState.Remove("Link");

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _businessLayer.CreateBanner(model);
        return Ok(result);
    }

    [HttpGet("list")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBanners()
    {
        var result = await _businessLayer.GetBanners();
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetBannerById(Guid id)
    {
        var result = await _businessLayer.GetBannerById(id);
        return Ok(result);
    }

    [HttpPut("update")]
    public async Task<IActionResult> UpdateBanner([FromForm] UpdateBannerModel model)
    {
        var result = await _businessLayer.UpdateBanner(model);
        return Ok(result);
    }

    [HttpDelete("delete/{id}")]
    public async Task<IActionResult> DeleteBanner(Guid id)
    {
        var result = await _businessLayer.DeleteBanner(id);
        return Ok(result);
    }
}