using BookingService.DTOs;

namespace BookingService.Services.TicketPdfGeneratorService;

public interface ITicketPdfGenerator
{
    byte[] GenerateTicketBytes(BookingResponseDto booking, string trainName, string source, string destination);
}