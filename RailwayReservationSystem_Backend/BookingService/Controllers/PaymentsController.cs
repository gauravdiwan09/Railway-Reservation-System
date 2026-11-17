using System.Security.Claims;
using BookingService.DTOs;
using BookingService.Services.BookingService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingService.Controllers;

[ApiController]
[Route("api/payments/razorpay")]
[Authorize]
public sealed class PaymentsController(IBookingService bookingService) : ControllerBase
{
    [HttpPost("orders")]
    public async Task<ActionResult<RazorpayOrderResponseDto>> CreateOrder(CreateRazorpayOrderRequestDto request)
    {
        var order = await bookingService.CreateRazorpayOrderAsync(request, GetCurrentUserId());
        return Ok(order);
    }

    [HttpPost("verify")]
    public async Task<ActionResult<RazorpayPaymentResponseDto>> VerifyPayment(VerifyRazorpayPaymentRequestDto request)
    {
        var payment = await bookingService.VerifyRazorpayPaymentAsync(request, GetCurrentUserId());
        return Ok(payment);
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim?.Value, out var userId))
            throw new UnauthorizedAccessException();
        return userId;
    }
}