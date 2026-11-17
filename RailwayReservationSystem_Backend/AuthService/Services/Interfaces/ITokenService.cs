using AuthService.Models;

namespace AuthService.Services.Interfaces;

public interface ITokenService
{
    string GenerateToken(User user);
}