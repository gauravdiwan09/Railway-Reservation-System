using AuthService.DTOs;
using AuthService.Exceptions;
using AuthService.Models;
using AuthService.Repositories.Interfaces;
using AuthService.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace AuthService.Tests;

public class AuthServiceTests
{
    [Test]
    public async Task RegisterAsync_WhenEmailAlreadyExists_ThrowsEmailAlreadyExistsException()
    {
        // Arrange
        var userRepository = new Mock<IUserRepository>();
        var roleRepository = new Mock<IRoleRepository>();
        var tokenService = new Mock<ITokenService>();
        var passwordHasher = new Mock<IPasswordHasher<User>>();

        userRepository
            .Setup(r => r.ExistsByEmailAsync("test@gmail.com"))
            .ReturnsAsync(true);

        var authService = new Services.AuthService(
            userRepository.Object,
            roleRepository.Object,
            tokenService.Object,
            passwordHasher.Object);

        var request = new RegisterRequestDto
        {
            FullName = "Test User",
            Email = "test@gmail.com",
            Password = "Password@123",
            Phone = "9876543210"
        };

        // Act & Assert
        Assert.ThrowsAsync<EmailAlreadyExistsException>(
            async () => await authService.RegisterAsync(request));
    }

    [Test]
public async Task RegisterAsync_WithValidRequest_CreatesPassengerUser()
{
    // Arrange
    var userRepository = new Mock<IUserRepository>();
    var roleRepository = new Mock<IRoleRepository>();
    var tokenService = new Mock<ITokenService>();
    var passwordHasher = new Mock<IPasswordHasher<User>>();

    var passengerRole = new Role
    {
        RoleId = 1,
        RoleName = "Passenger"
    };

    roleRepository
        .Setup(r => r.GetByNameAsync("Passenger"))
        .ReturnsAsync(passengerRole);

    userRepository
        .Setup(r => r.ExistsByEmailAsync("newuser@gmail.com"))
        .ReturnsAsync(false);

    passwordHasher
        .Setup(h => h.HashPassword(
            It.IsAny<User>(),
            "Password@123"))
        .Returns("hashed-password");

    userRepository
        .Setup(r => r.AddAsync(It.IsAny<User>()))
        .Returns(Task.CompletedTask);

    var authService = new Services.AuthService(
        userRepository.Object,
        roleRepository.Object,
        tokenService.Object,
        passwordHasher.Object);

    var request = new RegisterRequestDto
    {
        FullName = "New User",
        Email = "newuser@gmail.com",
        Password = "Password@123",
        Phone = "9876543210"
    };

    // Act
    var result = await authService.RegisterAsync(request);

    // Assert
    Assert.That(result.FullName, Is.EqualTo("New User"));
    Assert.That(result.Email, Is.EqualTo("newuser@gmail.com"));
    Assert.That(result.Phone, Is.EqualTo("9876543210"));
    Assert.That(result.RoleName, Is.EqualTo("Passenger"));

    userRepository.Verify(
        r => r.AddAsync(It.Is<User>(u =>
            u.Email == "newuser@gmail.com" &&
            u.RoleId == 1 &&
            u.PasswordHash == "hashed-password")),
        Times.Once);
}

[Test]
public void RegisterAsync_WhenPassengerRoleDoesNotExist_ThrowsInvalidOperationException()
{
    // Arrange
    var userRepository = new Mock<IUserRepository>();
    var roleRepository = new Mock<IRoleRepository>();
    var tokenService = new Mock<ITokenService>();
    var passwordHasher = new Mock<IPasswordHasher<User>>();

    userRepository
        .Setup(r => r.ExistsByEmailAsync("newuser@gmail.com"))
        .ReturnsAsync(false);

    roleRepository
        .Setup(r => r.GetByNameAsync("Passenger"))
        .ReturnsAsync((Role?)null);

    var authService = new Services.AuthService(
        userRepository.Object,
        roleRepository.Object,
        tokenService.Object,
        passwordHasher.Object);

    var request = new RegisterRequestDto
    {
        FullName = "New User",
        Email = "newuser@gmail.com",
        Password = "Password@123",
        Phone = "9876543210"
    };

    // Act & Assert
    var exception = Assert.ThrowsAsync<InvalidOperationException>(
        async () => await authService.RegisterAsync(request));

    Assert.That(
        exception!.Message,
        Is.EqualTo("Passenger role is not configured."));
}

[Test]
public void LoginAsync_WhenEmailDoesNotExist_ThrowsInvalidCredentialsException()
{
    // Arrange
    var userRepository = new Mock<IUserRepository>();
    var roleRepository = new Mock<IRoleRepository>();
    var tokenService = new Mock<ITokenService>();
    var passwordHasher = new Mock<IPasswordHasher<User>>();

    userRepository
        .Setup(r => r.GetByEmailAsync("unknown@gmail.com"))
        .ReturnsAsync((User?)null);

    var authService = new Services.AuthService(
        userRepository.Object,
        roleRepository.Object,
        tokenService.Object,
        passwordHasher.Object);

    var request = new LoginRequestDto
    {
        Email = "unknown@gmail.com",
        Password = "Password@123"
    };

    // Act & Assert
    var exception = Assert.ThrowsAsync<InvalidCredentialsException>(
        async () => await authService.LoginAsync(request));

    Assert.That(
        exception!.Message,
        Is.EqualTo("Invalid email or password."));
}

[Test]
public void LoginAsync_WhenPasswordIsWrong_ThrowsInvalidCredentialsException()
{
    // Arrange
    var userRepository = new Mock<IUserRepository>();
    var roleRepository = new Mock<IRoleRepository>();
    var tokenService = new Mock<ITokenService>();
    var passwordHasher = new Mock<IPasswordHasher<User>>();

    var user = new User
    {
        UserId = 1,
        Email = "test@gmail.com",
        PasswordHash = "hashed-password",
        Role = new Role
        {
            RoleId = 1,
            RoleName = "Passenger"
        }
    };

    userRepository
        .Setup(r => r.GetByEmailAsync("test@gmail.com"))
        .ReturnsAsync(user);

    passwordHasher
        .Setup(h => h.VerifyHashedPassword(
            user,
            "hashed-password",
            "WrongPassword"))
        .Returns(PasswordVerificationResult.Failed);

    var authService = new Services.AuthService(
        userRepository.Object,
        roleRepository.Object,
        tokenService.Object,
        passwordHasher.Object);

    var request = new LoginRequestDto
    {
        Email = "test@gmail.com",
        Password = "WrongPassword"
    };

    // Act & Assert
    Assert.ThrowsAsync<InvalidCredentialsException>(
        async () => await authService.LoginAsync(request));
}

[Test]
public async Task LoginAsync_WithValidCredentials_ReturnsToken()
{
    // Arrange
    var userRepository = new Mock<IUserRepository>();
    var roleRepository = new Mock<IRoleRepository>();
    var tokenService = new Mock<ITokenService>();
    var passwordHasher = new Mock<IPasswordHasher<User>>();

    var user = new User
    {
        UserId = 1,
        Email = "test@gmail.com",
        PasswordHash = "hashed-password",
        Role = new Role
        {
            RoleId = 1,
            RoleName = "Passenger"
        }
    };

    userRepository
        .Setup(r => r.GetByEmailAsync("test@gmail.com"))
        .ReturnsAsync(user);

    passwordHasher
        .Setup(h => h.VerifyHashedPassword(
            user,
            "hashed-password",
            "Password@123"))
        .Returns(PasswordVerificationResult.Success);

    tokenService
        .Setup(t => t.GenerateToken(user))
        .Returns("test-jwt-token");

    var authService = new Services.AuthService(
        userRepository.Object,
        roleRepository.Object,
        tokenService.Object,
        passwordHasher.Object);

    var request = new LoginRequestDto
    {
        Email = "test@gmail.com",
        Password = "Password@123"
    };

    // Act
    var result = await authService.LoginAsync(request);

    // Assert
    Assert.That(result.Token, Is.EqualTo("test-jwt-token"));

    tokenService.Verify(
        t => t.GenerateToken(user),
        Times.Once);
}

[Test]
public async Task GetUserByIdAsync_WhenUserExists_ReturnsUserResponse()
{
    // Arrange
    var userRepository = new Mock<IUserRepository>();
    var roleRepository = new Mock<IRoleRepository>();
    var tokenService = new Mock<ITokenService>();
    var passwordHasher = new Mock<IPasswordHasher<User>>();

    var user = new User
    {
        UserId = 1,
        FullName = "Test Passenger",
        Email = "passenger@test.com",
        Phone = "9876543210",
        PasswordHash = "hashed-password",
        RoleId = 1,
        CreatedAt = DateTime.UtcNow,
        Role = new Role
        {
            RoleId = 1,
            RoleName = "Passenger"
        }
    };

    userRepository
        .Setup(r => r.GetByIdAsync(1))
        .ReturnsAsync(user);

    var authService = new Services.AuthService(
        userRepository.Object,
        roleRepository.Object,
        tokenService.Object,
        passwordHasher.Object);

    // Act
    var result = await authService.GetUserByIdAsync(1);

    // Assert
    Assert.That(result.UserId, Is.EqualTo(1));
    Assert.That(result.FullName, Is.EqualTo("Test Passenger"));
    Assert.That(result.Email, Is.EqualTo("passenger@test.com"));
    Assert.That(result.Phone, Is.EqualTo("9876543210"));
    Assert.That(result.RoleName, Is.EqualTo("Passenger"));

    userRepository.Verify(
        r => r.GetByIdAsync(1),
        Times.Once);
}

[Test]
public void GetUserByIdAsync_WhenUserDoesNotExist_ThrowsResourceNotFoundException()
{
    // Arrange
    var userRepository = new Mock<IUserRepository>();
    var roleRepository = new Mock<IRoleRepository>();
    var tokenService = new Mock<ITokenService>();
    var passwordHasher = new Mock<IPasswordHasher<User>>();

    userRepository
        .Setup(r => r.GetByIdAsync(999))
        .ReturnsAsync((User?)null);

    var authService = new Services.AuthService(
        userRepository.Object,
        roleRepository.Object,
        tokenService.Object,
        passwordHasher.Object);

    // Act & Assert
    var exception = Assert.ThrowsAsync<ResourceNotFoundException>(
        async () => await authService.GetUserByIdAsync(999));

    Assert.That(
        exception!.Message,
        Is.EqualTo("User not found."));
}

[Test]
public async Task UpdateUserAsync_WhenUserExists_UpdatesUserAndReturnsResponse()
{
    // Arrange
    var userRepository = new Mock<IUserRepository>();
    var roleRepository = new Mock<IRoleRepository>();
    var tokenService = new Mock<ITokenService>();
    var passwordHasher = new Mock<IPasswordHasher<User>>();

    var user = new User
    {
        UserId = 1,
        FullName = "Old Name",
        Email = "test@gmail.com",
        Phone = "1111111111",
        PasswordHash = "hashed-password",
        RoleId = 1,
        CreatedAt = DateTime.UtcNow,
        Role = new Role
        {
            RoleId = 1,
            RoleName = "Passenger"
        }
    };

    userRepository
        .Setup(r => r.GetByIdAsync(1))
        .ReturnsAsync(user);

    userRepository
        .Setup(r => r.UpdateAsync(It.IsAny<User>()))
        .Returns(Task.CompletedTask);

    var authService = new Services.AuthService(
        userRepository.Object,
        roleRepository.Object,
        tokenService.Object,
        passwordHasher.Object);

    var request = new UpdateUserRequestDto
    {
        FullName = "Updated Name",
        Phone = "9999999999"
    };

    // Act
    var result = await authService.UpdateUserAsync(1, request);

    // Assert
    Assert.That(result.FullName, Is.EqualTo("Updated Name"));
    Assert.That(result.Phone, Is.EqualTo("9999999999"));
    Assert.That(result.Email, Is.EqualTo("test@gmail.com"));

    userRepository.Verify(
        r => r.UpdateAsync(It.Is<User>(u =>
            u.FullName == "Updated Name" &&
            u.Phone == "9999999999")),
        Times.Once);
}

[Test]
public void UpdateUserAsync_WhenUserDoesNotExist_ThrowsResourceNotFoundException()
{
    // Arrange
    var userRepository = new Mock<IUserRepository>();
    var roleRepository = new Mock<IRoleRepository>();
    var tokenService = new Mock<ITokenService>();
    var passwordHasher = new Mock<IPasswordHasher<User>>();

    userRepository
        .Setup(r => r.GetByIdAsync(999))
        .ReturnsAsync((User?)null);

    var authService = new Services.AuthService(
        userRepository.Object,
        roleRepository.Object,
        tokenService.Object,
        passwordHasher.Object);

    var request = new UpdateUserRequestDto
    {
        FullName = "Updated Name",
        Phone = "9999999999"
    };

    // Act & Assert
    var exception = Assert.ThrowsAsync<ResourceNotFoundException>(
        async () => await authService.UpdateUserAsync(999, request));

    Assert.That(
        exception!.Message,
        Is.EqualTo("User not found."));
}

[Test]
public async Task DeleteUserAsync_WhenUserExists_DeletesUser()
{
    // Arrange
    var userRepository = new Mock<IUserRepository>();
    var roleRepository = new Mock<IRoleRepository>();
    var tokenService = new Mock<ITokenService>();
    var passwordHasher = new Mock<IPasswordHasher<User>>();

    var user = new User
    {
        UserId = 1,
        FullName = "Test User",
        Email = "test@gmail.com",
        PasswordHash = "hashed-password",
        RoleId = 1,
        Role = new Role
        {
            RoleId = 1,
            RoleName = "Passenger"
        }
    };

    userRepository
        .Setup(r => r.GetByIdAsync(1))
        .ReturnsAsync(user);

    userRepository
        .Setup(r => r.DeleteAsync(1))
        .Returns(Task.CompletedTask);

    var authService = new Services.AuthService(
        userRepository.Object,
        roleRepository.Object,
        tokenService.Object,
        passwordHasher.Object);

    // Act
    await authService.DeleteUserAsync(1);

    // Assert
    userRepository.Verify(
        r => r.DeleteAsync(1),
        Times.Once);
}

[Test]
public async Task GetRolesAsync_WhenRolesExist_ReturnsRoleResponses()
{
    // Arrange
    var userRepository = new Mock<IUserRepository>();
    var roleRepository = new Mock<IRoleRepository>();
    var tokenService = new Mock<ITokenService>();
    var passwordHasher = new Mock<IPasswordHasher<User>>();

    var roles = new List<Role>
    {
        new Role
        {
            RoleId = 1,
            RoleName = "Passenger"
        },
        new Role
        {
            RoleId = 2,
            RoleName = "Admin"
        }
    };

    roleRepository
        .Setup(r => r.GetAllAsync())
        .ReturnsAsync(roles);

    var authService = new Services.AuthService(
        userRepository.Object,
        roleRepository.Object,
        tokenService.Object,
        passwordHasher.Object);

    // Act
    var result = await authService.GetRolesAsync();

    // Assert
    Assert.That(result.Count, Is.EqualTo(2));
    Assert.That(result[0].RoleName, Is.EqualTo("Passenger"));
    Assert.That(result[1].RoleName, Is.EqualTo("Admin"));

    roleRepository.Verify(
        r => r.GetAllAsync(),
        Times.Once);
}
}