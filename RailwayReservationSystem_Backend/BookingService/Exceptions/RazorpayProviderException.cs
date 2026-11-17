namespace BookingService.Exceptions;

public sealed class RazorpayProviderException(string message) : Exception(message);