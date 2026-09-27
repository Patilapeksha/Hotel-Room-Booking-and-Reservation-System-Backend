using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HotelBooking.Api.Data;
using HotelBooking.Api.DTOs.Auth;
using HotelBooking.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace HotelBooking.Api.Services;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly PasswordHasher<User> _passwordHasher;

    public AuthService(
        AppDbContext db,
        IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
        _passwordHasher = new PasswordHasher<User>();
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(x => x.Email == request.Email);

        if (user == null || user.Status != "Active")
            return null;

        var result = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);

        if (result == PasswordVerificationResult.Failed)
            return null;

        var refreshToken = await CreateRefreshTokenAsync(user);

        return new AuthResponse
        {
            AccessToken = GenerateAccessToken(user),
            RefreshToken = refreshToken,
            UserId = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role
        };
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
    {
        var existingUser = await _db.Users
            .FirstOrDefaultAsync(x => x.Email == request.Email);

        if (existingUser != null)
            return null;

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            Role = "Guest",
            Status = "Active"
        };

        user.PasswordHash = _passwordHasher.HashPassword(
            user,
            request.Password);

        _db.Users.Add(user);

        await _db.SaveChangesAsync();

        var refreshToken = await CreateRefreshTokenAsync(user);

        return new AuthResponse
        {
            AccessToken = GenerateAccessToken(user),
            RefreshToken = refreshToken,
            UserId = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role
        };
    }

    public async Task<AuthResponse?> RefreshAsync(
        string refreshToken)
    {
        var storedToken = await _db.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Token == refreshToken);

        if (storedToken == null)
            return null;

        if (storedToken.IsRevoked)
            return null;

        if (storedToken.ExpiresAt <= DateTime.UtcNow)
            return null;

        if (storedToken.User.Status != "Active")
            return null;

        // Revoke old refresh token
        storedToken.IsRevoked = true;

        // Create a new refresh token
        var newRefreshToken =
            await CreateRefreshTokenAsync(storedToken.User);

        await _db.SaveChangesAsync();

        return new AuthResponse
        {
            AccessToken =
                GenerateAccessToken(storedToken.User),

            RefreshToken = newRefreshToken,

            UserId = storedToken.User.Id,
            Name = storedToken.User.Name,
            Email = storedToken.User.Email,
            Role = storedToken.User.Role
        };
    }

    private async Task<string> CreateRefreshTokenAsync(User user)
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);

        var token = Convert.ToBase64String(randomBytes);

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };

        _db.RefreshTokens.Add(refreshToken);

        await _db.SaveChangesAsync();

        return token;
    }

    private string GenerateAccessToken(User user)
    {
        var jwtKey = _configuration["Jwt:Key"];

        if (string.IsNullOrWhiteSpace(jwtKey))
            throw new InvalidOperationException(
                "JWT key is not configured.");

        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];

        var claims = new[]
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                user.Id.ToString()),

            new Claim(
                ClaimTypes.Name,
                user.Name),

            new Claim(
                ClaimTypes.Email,
                user.Email),

            new Claim(
                ClaimTypes.Role,
                user.Role)
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }
}