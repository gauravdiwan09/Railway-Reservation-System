using AuthService.DTOs;
using AuthService.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize(Roles = "Admin")]
public class RolesController : ControllerBase
{
    private readonly IAuthService _authService;

    public RolesController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpGet]
    public async Task<ActionResult<List<RoleResponseDto>>> GetRoles()
    {
        var roles = await _authService.GetRolesAsync();

        return Ok(roles);
    }
}