namespace BookingService.Services.PnrGeneratorService;

public interface IPnrGeneratorService
{
    Task<string> GenerateAsync();
}