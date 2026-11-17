using AuthService.DTOs;

namespace AuthService.Services.Interfaces;

public interface IAuthService
{
    Task<UserResponseDto> RegisterAsync(RegisterRequestDto request);

    Task<LoginResponseDto> LoginAsync(LoginRequestDto request);

    Task<UserResponseDto> GetUserByIdAsync(int userId);

    Task<UserResponseDto> UpdateUserAsync(
        int userId,
        UpdateUserRequestDto request);

    Task<List<RoleResponseDto>> GetRolesAsync();

    Task DeleteUserAsync(int userId);
}