using AuthService.DTOs;
using AuthService.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Controllers;

[ApiController]
[Route("api")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("auth/register")]
    public async Task<ActionResult<UserResponseDto>> Register(
        RegisterRequestDto request)
    {
            var result = await _authService.RegisterAsync(request);

            return Ok(result);
    }

    [HttpPost("auth/login")]
    public async Task<ActionResult<LoginResponseDto>> Login(
        LoginRequestDto request)
    {
            var result = await _authService.LoginAsync(request);

            return Ok(result);
    }

    
}