using AuthService.DTOs;
using AuthService.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace AuthService.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IAuthService _authService;

    public UsersController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserResponseDto>> GetUser(int id)
    {
        var currentUserIdStr = User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (currentUserIdStr is null)
        {
            return Forbid();
        }

        var currentUserId = int.Parse(currentUserIdStr);

        bool isAdmin = User.IsInRole("Admin");

        if (!isAdmin && currentUserId != id)
        {
            return Forbid();
        }

        var user = await _authService.GetUserByIdAsync(id);

        return Ok(user);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserResponseDto>> UpdateUser(
        int id,
        UpdateUserRequestDto request)
    {
        var currentUserIdStr = User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (currentUserIdStr is null)
        {
            return Forbid();
        }

        var currentUserId = int.Parse(currentUserIdStr);

        bool isAdmin = User.IsInRole("Admin");

        if (!isAdmin && currentUserId != id)
        {
            return Forbid();
        }

        var user = await _authService.UpdateUserAsync(id, request);

        return Ok(user);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var currentUserIdStr = User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (currentUserIdStr is null)
        {
            return Forbid();
        }

        var currentUserId = int.Parse(currentUserIdStr);

        bool isAdmin = User.IsInRole("Admin");

        if (!isAdmin && currentUserId != id)
        {
            return Forbid();
        }

        await _authService.DeleteUserAsync(id);

        return NoContent();
    }
}