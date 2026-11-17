namespace BookingService.Exceptions;

public class InvalidBookingException : Exception
{
    public InvalidBookingException(string message)
        : base(message)
    {
    }
}