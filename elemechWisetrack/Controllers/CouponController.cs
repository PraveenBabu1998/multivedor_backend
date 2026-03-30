using elemechWisetrack.BusinessLayer;
using elemechWisetrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[ApiController]
[Route("api/coupon")]
[Authorize]
public class CouponController : ControllerBase
{
    private readonly IBusinessLayer _businessLayer;

    public CouponController(IBusinessLayer businessLayer)
    {
        _businessLayer = businessLayer;
    }

    private string GetUserEmail()
    {
        return User.FindFirst(ClaimTypes.Email)?.Value ??
               User.FindFirst("UserName")?.Value ??
               User.FindFirst("email")?.Value;
    }

    // ✅ Create
    [HttpPost("add")]
    public async Task<IActionResult> CreateCoupon([FromBody] CouponCodeCreate request)
    {
        var result = await _businessLayer.CreateCoupon(GetUserEmail(), request);
        return Ok(result);
    }

    // ✅ Get All
    [HttpGet("list")]
    public async Task<IActionResult> GetCoupons()
    {
        var result = await _businessLayer.GetCoupons();
        return Ok(result);
    }

    // ✅ Get By Id
    [HttpGet("{id}")]
    public async Task<IActionResult> GetCouponById(Guid id)
    {
        var result = await _businessLayer.GetCouponById(id);
        return Ok(result);
    }

    // ✅ Update
    [HttpPut("update/{id}")]
    public async Task<IActionResult> UpdateCoupon(Guid id, [FromBody] CouponCodeCreate request)
    {
        var result = await _businessLayer.UpdateCoupon(id, request);
        return Ok(result);
    }

    // ✅ Delete
    [HttpDelete("delete/{id}")]
    public async Task<IActionResult> DeleteCoupon(Guid id)
    {
        var result = await _businessLayer.DeleteCoupon(id);
        return Ok(result);
    }

    // ✅ Apply Coupon
    
    [HttpPost("apply")]
    [AllowAnonymous]
    public async Task<IActionResult> ApplyCoupon([FromBody] ApplyCouponRequest request)
    {
        var result = await _businessLayer.ApplyCoupon(GetUserEmail(), request);
        return Ok(result);
    }

    [HttpGet("usage-list")]
    public async Task<IActionResult> GetCouponUsage()
    {
        var result = await _businessLayer.GetCouponUsage();
        return Ok(result);
    }
}