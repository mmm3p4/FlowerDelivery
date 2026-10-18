using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FlowerDelivery.Infrastructure.Data;
using FlowerDelivery.Domain.Entities;

namespace FlowerDelivery.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AuthController(ApplicationDbContext context)
    {
        _context = context;
    }

    // POST: api/auth/register
    [HttpPost("register")]
    public async Task<ActionResult<User>> Register([FromBody] RegisterDto dto)
    {
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (existingUser != null)
        {
            return BadRequest(new { message = "Пользователь с таким email уже существует" });
        }

        var user = new User
        {
            LastName = dto.LastName,
            FirstName = dto.FirstName,
            MiddleName = dto.MiddleName,
            Email = dto.Email,
            PasswordHash = dto.Password,
            Role = dto.Role
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Регистрация успешна", userId = user.Id });
    }

    // POST: api/auth/login
    [HttpPost("login")]
    public async Task<ActionResult> Login([FromBody] LoginDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (user == null || user.PasswordHash != dto.Password)
        {
            return Unauthorized(new { message = "Неверный email или пароль" });
        }

        return Ok(new
        {
            message = "Авторизация успешна",
            userId = user.Id,
            role = user.Role,
            name = $"{user.LastName} {user.FirstName}"
        });
    }
}

public class RegisterDto
{
    public string LastName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string MiddleName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "Client";
}

public class LoginDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}