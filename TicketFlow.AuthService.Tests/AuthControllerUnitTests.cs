using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TicketFlow.AuthService.Controllers;
using TicketFlow.AuthService.Data;
using TicketFlow.AuthService.DTOs;
using TicketFlow.AuthService.Models;
using TicketFlow.AuthService.Services;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

namespace TicketFlow.AuthService.Tests;

public class AuthControllerUnitTests
{
    private AuthDbContext CreateInMemoryDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new AuthDbContext(options);
    }

    private ITokenService CreateTokenService()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "TicketFlow_SuperSecret_Dev_Key_MinLength32Chars!",
                ["Jwt:Issuer"] = "TicketFlow.AuthService",
                ["Jwt:Audience"] = "TicketFlow",
                ["Jwt:ExpiryMinutes"] = "60"
            })
            .Build();
        return new TokenService(config);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        // Arrange
        using var db = CreateInMemoryDb("Register_Duplicate");
        var tokenService = CreateTokenService();
        var controller = new AuthController(db, tokenService);

        var request = new RegisterRequest("test@example.com", "password123");

        // Act - First register
        await controller.Register(request);

        // Act - Second register (duplicate)
        var result = await controller.Register(request);

        // Assert
        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        // Arrange
        using var db = CreateInMemoryDb("Login_WrongPassword");
        var tokenService = CreateTokenService();
        var controller = new AuthController(db, tokenService);

        // Register a user first
        await controller.Register(new RegisterRequest("user@example.com", "correctpassword"));

        // Act - login with wrong password
        var result = await controller.Login(new LoginRequest("user@example.com", "wrongpassword"));

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_ValidCredentials_TokenContainsCorrectClaims()
    {
        // Arrange
        using var db = CreateInMemoryDb("Login_ValidClaims");
        var tokenService = CreateTokenService();
        var controller = new AuthController(db, tokenService);

        await controller.Register(new RegisterRequest("admin@example.com", "password123"));

        // Act
        var result = await controller.Login(new LoginRequest("admin@example.com", "password123"));

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var loginResponse = Assert.IsType<LoginResponse>(okResult.Value);

        // Decode JWT and verify claims
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(loginResponse.Token);

        Assert.NotEmpty(token.Claims.First(c => c.Type == "sub").Value);
        Assert.Equal("admin@example.com", token.Claims.First(c => c.Type == "email").Value);
        Assert.NotEmpty(token.Claims.First(c => c.Type == "role").Value);
    }
}
