using FlowerDelivery.Application.DTOs;
using FlowerDelivery.Application.Services;
using FlowerDelivery.Domain.Entities;
using FlowerDelivery.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FlowerDelivery.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(ApplicationDbContext context, IConfiguration configuration) : ControllerBase
{
    private readonly ApplicationDbContext _context = context;
    private readonly IConfiguration _configuration = configuration;

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto dto)
    {
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (existingUser != null)
        {
            return BadRequest(new { message = "Пользователь с таким email уже существует" });
        }

        string hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        User newUser;

        if (dto.Role == "Client")
        {
            newUser = new Client
            {
                LastName = dto.LastName,
                FirstName = dto.FirstName,
                MiddleName = dto.MiddleName ?? string.Empty,
                Email = dto.Email,
                PasswordHash = hashedPassword,
                Phone = dto.Phone ?? string.Empty,
                DefaultAddress = dto.DefaultAddress ?? string.Empty
            };
        }
        else 
        {
            newUser = new Courier
            {
                LastName = dto.LastName,
                FirstName = dto.FirstName,
                MiddleName = dto.MiddleName ?? string.Empty,
                Email = dto.Email,
                PasswordHash = hashedPassword,
                Phone = dto.Phone ?? string.Empty,
                Status = CourierStatus.Free
            };
        }

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        var token = GenerateJwtToken(newUser);

        return Ok(new AuthResponseDto
        {
            UserId = newUser.Id,
            Email = newUser.Email,
            FullName = $"{newUser.LastName} {newUser.FirstName}".Trim(),
            Role = newUser.Role, 
            Token = token
        });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Неверный email или пароль" });
        }

        var token = GenerateJwtToken(user);

        return Ok(new AuthResponseDto
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = $"{user.LastName} {user.FirstName}".Trim(),
            Role = user.Role,
            Token = token
        });
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