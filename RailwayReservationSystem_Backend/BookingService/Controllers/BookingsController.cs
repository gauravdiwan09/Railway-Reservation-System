using BookingService.DTOs;
using BookingService.Services.TicketPdfGeneratorService;
using BookingService.Services.TrainServiceClientService;
using BookingService.Services.BookingService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BookingService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;
    private readonly ITicketPdfGenerator _ticketPdfGenerator;
    private readonly ITrainServiceClient _trainServiceClient;

    public BookingsController(IBookingService bookingService, ITicketPdfGenerator ticketPdfGenerator, ITrainServiceClient trainServiceClient)
    {
        _bookingService = bookingService;
        _ticketPdfGenerator = ticketPdfGenerator;
        _trainServiceClient = trainServiceClient;
    }

    [HttpPost]
    public async Task<ActionResult<BookingResponseDto>> CreateBooking(
        CreateBookingRequestDto request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        //same logic we are using in other service as well.
        if (userIdClaim == null)
            return Unauthorized();

        var userId = int.Parse(userIdClaim.Value);

        var booking = await _bookingService.CreateBookingAsync(
            request,
            userId);

        return Ok(booking);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BookingResponseDto>> GetBooking(int id)
    {
        var booking = await _bookingService.GetBookingByIdAsync(id);

        if (booking.UserId != GetCurrentUserId())
        return Forbid();

        return Ok(booking);
    }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<List<BookingResponseDto>>> GetUserBookings(
        int userId)
    {
        if (userId != GetCurrentUserId())
        return Forbid();

        var bookings = await _bookingService.GetUserBookingsAsync(userId);

        return Ok(bookings);
    }

    [HttpGet("pnr/{pnr}")]
    [AllowAnonymous]
    public async Task<ActionResult<BookingStatusResponseDto>> GetBookingByPNR(
        string pnr)
    {
        var booking = await _bookingService.GetBookingByPNRAsync(pnr);

        return Ok(new BookingStatusResponseDto
        {
            PNR = booking.PNR,
            JourneyDate = booking.JourneyDate,
            Status = booking.Status
        });
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> CancelBooking(int id)
    {   
        var booking = await _bookingService.GetBookingByIdAsync(id);

        if (booking.UserId != GetCurrentUserId())
        return Forbid();

        var cancellation = await _bookingService.CancelBookingAsync(id);
        return Ok(cancellation);
    }

    [HttpGet("ticket/{id}/download")]
    [AllowAnonymous]
    public async Task<IActionResult> DownloadTicket(int id) //all functionality is in controller itself
    {
        var booking = await _bookingService.GetBookingByIdAsync(id);

        if (booking == null)
            return NotFound("Booking not found.");

        var train = await _trainServiceClient.GetTrainAsync(booking.TrainId);

        if (train == null)
            return NotFound("Train not found.");

        var pdfBytes = _ticketPdfGenerator.GenerateTicketBytes(
            booking,
            train.TrainName,
            string.IsNullOrWhiteSpace(booking.Source) ? train.Source : booking.Source,
            string.IsNullOrWhiteSpace(booking.Destination) ? train.Destination : booking.Destination);

        Response.Headers.Append(
            "Content-Disposition",
            $"inline; filename=RailwayTicket_{booking.PNR}.pdf");

        return File(
            pdfBytes,
            "application/pdf");
    }
    //helper method
    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

        if (userIdClaim == null)
            throw new UnauthorizedAccessException();

        return int.Parse(userIdClaim.Value);
    }
}