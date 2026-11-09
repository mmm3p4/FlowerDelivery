using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FlowerDelivery.Application.DTOs;
using FlowerDelivery.Domain.Entities;
using FlowerDelivery.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using BCrypt.Net;
using Microsoft.Extensions.Configuration;

namespace FlowerDelivery.Application.Services;

public class AuthService(ApplicationDbContext context, IConfiguration configuration)
{
    private readonly ApplicationDbContext _context = context;
    private readonly IConfiguration _configuration = configuration;

    public async Task<AuthResponseDto?> RegisterAsync(RegisterDto dto)
    {
        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
        {
            throw new InvalidOperationException("Пользователь с таким email уже существует");
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        User user;

        if (dto.Role == "Client")
        {
            var client = new Client
            {
                LastName = dto.LastName,
                FirstName = dto.FirstName,
                MiddleName = dto.MiddleName ?? string.Empty,
                Email = dto.Email,
                PasswordHash = passwordHash,
                Role = "Client",
                Phone = dto.Phone ?? string.Empty,
                DefaultAddress = dto.DefaultAddress ?? string.Empty
            };
            user = client;
        }
        else if (dto.Role == "Courier") 
        {
            var courier = new Courier
            {
                LastName = dto.LastName,
                FirstName = dto.FirstName,
                MiddleName = dto.MiddleName ?? string.Empty,
                Email = dto.Email,
                PasswordHash = passwordHash,
                Role = "Courier",
                Phone = dto.Phone ?? string.Empty,
                Status = CourierStatus.Free
            };
            user = courier;
        }
        else 
        {
            user = new User
            {
                LastName = dto.LastName,
                FirstName = dto.FirstName,
                MiddleName = dto.MiddleName ?? string.Empty,
                Email = dto.Email,
                PasswordHash = passwordHash,
                Role = "Admin"
            };
        }

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        user = await _context.Users.FindAsync(user.Id);

        var token = GenerateJwtToken(user);

        return new AuthResponseDto
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = $"{user.LastName} {user.FirstName} {user.MiddleName}".Trim(),
            Role = user.Role,
            Token = token
        };
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            return null;
        }

        var token = GenerateJwtToken(user);

        return new AuthResponseDto
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = $"{user.LastName} {user.FirstName} {user.MiddleName}".Trim(),
            Role = user.Role,
            Token = token
        };
    }

    private string GenerateJwtToken(User user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration["Jwt:Key"] ?? "YourSuperSecretKey12345678901234567890"));

        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("FullName", $"{user.LastName} {user.FirstName}")
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "FlowerDelivery",
            audience: _configuration["Jwt:Audience"] ?? "FlowerDelivery",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}