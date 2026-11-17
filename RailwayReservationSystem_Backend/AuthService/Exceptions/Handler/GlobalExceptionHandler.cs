using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Exceptions.Handler;

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
        _logger.LogError(
            exception,
            "An unhandled exception occurred.");

        var statusCode = exception switch
        {
            EmailAlreadyExistsException =>
                StatusCodes.Status409Conflict,

            InvalidCredentialsException =>
                StatusCodes.Status401Unauthorized,

            ResourceNotFoundException =>
                StatusCodes.Status404NotFound,

            _ =>
                StatusCodes.Status500InternalServerError
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = statusCode switch
            {
                StatusCodes.Status409Conflict =>
                    "Email already exists.",

                StatusCodes.Status401Unauthorized =>
                    "Invalid credentials.",

                StatusCodes.Status404NotFound =>
                    "Resource not found.",

                _ =>
                    "An unexpected error occurred."
            },
            Detail = statusCode == StatusCodes.Status500InternalServerError ? "An unexpected error occured while processing your request." : exception.Message
        };

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            cancellationToken);

        return true;
    }
}