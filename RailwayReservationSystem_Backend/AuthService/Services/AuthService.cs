using AuthService.DTOs;
using AuthService.Models;
using AuthService.Repositories.Interfaces;
using Microsoft.AspNetCore.Identity;
using AuthService.Services.Interfaces;
using AuthService.Exceptions;

namespace AuthService.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher<User> _passwordHasher;

    public AuthService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        ITokenService tokenService,
        IPasswordHasher<User> passwordHasher)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
    }

    public async Task<UserResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        var emailExists = await _userRepository.ExistsByEmailAsync(request.Email);

        if (emailExists)
        {
            throw new EmailAlreadyExistsException("Email is already registered.");
        }

        var passengerRole = await _roleRepository.GetByNameAsync("Passenger");

        if (passengerRole is null)
        {
            throw new InvalidOperationException(
                "Passenger role is not configured.");
        }

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            Phone = request.Phone,
            CreatedAt = DateTime.UtcNow,
            RoleId = passengerRole.RoleId
        };

        user.PasswordHash =
            _passwordHasher.HashPassword(user, request.Password);

        await _userRepository.AddAsync(user);

        return new UserResponseDto
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            CreatedAt = user.CreatedAt,
            RoleName = passengerRole.RoleName
        };
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);

        if (user is null)
        {
            throw new InvalidCredentialsException("Invalid email or password.");
        }

        var passwordResult = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);

        if (passwordResult == PasswordVerificationResult.Failed)
        {
            throw new InvalidCredentialsException("Invalid email or password.");
        }

        var token = _tokenService.GenerateToken(user);

        return new LoginResponseDto
        {
            Token = token
        };
    }

    public async Task<UserResponseDto> GetUserByIdAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);

        if (user is null)
        {
            throw new ResourceNotFoundException("User not found.");
        }

        return new UserResponseDto
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            CreatedAt = user.CreatedAt,
            RoleName = user.Role.RoleName
        };
    }

        public async Task<UserResponseDto> UpdateUserAsync( //then we don't need ? mark as null values are now handled.
        int userId,
        UpdateUserRequestDto request)
    {
        var user = await _userRepository.GetByIdAsync(userId);

        if (user is null)
        {
            throw new ResourceNotFoundException("User not found.");
        }

        user.FullName = request.FullName;
        user.Phone = request.Phone;

        await _userRepository.UpdateAsync(user);

        return new UserResponseDto
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            CreatedAt = user.CreatedAt,
            RoleName = user.Role.RoleName
        };
    }

        public async Task<List<RoleResponseDto>> GetRolesAsync()
    {
        var roles = await _roleRepository.GetAllAsync();

        return roles.Select(role => new RoleResponseDto
        {
            RoleId = role.RoleId,
            RoleName = role.RoleName
        }).ToList();
    }

        public async Task DeleteUserAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);

            if (user is null)
            {
                throw new ResourceNotFoundException(
                    "User not found.");
            }

            await _userRepository.DeleteAsync(userId);
        }
}