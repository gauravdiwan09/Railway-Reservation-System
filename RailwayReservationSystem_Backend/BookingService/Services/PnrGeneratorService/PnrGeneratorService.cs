using BookingService.Repositories.Interfaces;
using BookingService.Services.PnrGeneratorService;

namespace BookingService.Services.PnrGeneratorService;

public class PnrGeneratorService : IPnrGeneratorService
{
    private readonly IBookingRepository _bookingRepository;

    public PnrGeneratorService(IBookingRepository bookingRepository)
    {
        _bookingRepository = bookingRepository;
    }

    public async Task<string> GenerateAsync()
    {
        string pnr;

        do
        {
            pnr = $"PNR{Random.Shared.Next(100000, 1000000)}";
        }
        while (await _bookingRepository.ExistsByPNRAsync(pnr));

        return pnr;
    }
}