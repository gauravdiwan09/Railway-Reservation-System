namespace BookingService.Exceptions;

public sealed class RazorpayConfigurationException(string message) : Exception(message);