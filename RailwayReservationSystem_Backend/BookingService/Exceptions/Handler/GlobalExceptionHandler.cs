using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace BookingService.Exceptions.Handler;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "An exception occurred.");

        var statusCode = exception switch
        {
            BookingNotFoundException => StatusCodes.Status404NotFound,
            BookingConflictException => StatusCodes.Status409Conflict,
            InvalidBookingException => StatusCodes.Status400BadRequest,
            RazorpayConfigurationException => StatusCodes.Status503ServiceUnavailable,
            RazorpayProviderException => StatusCodes.Status502BadGateway,
            _ => StatusCodes.Status500InternalServerError
        };

        var detail = statusCode == StatusCodes.Status500InternalServerError
            ? "An unexpected error occurred."
            : exception.Message;

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = statusCode,
                Title = statusCode switch
                {
                    StatusCodes.Status400BadRequest => "Invalid booking request",
                    StatusCodes.Status404NotFound => "Booking not found",
                    StatusCodes.Status409Conflict => "Booking conflict",
                    StatusCodes.Status503ServiceUnavailable => "Payment gateway is not configured",
                    StatusCodes.Status502BadGateway => "Payment gateway error",
                    _ => "Internal Server Error"
                },
                Detail = detail
            },
            cancellationToken);

        return true;
    }
}