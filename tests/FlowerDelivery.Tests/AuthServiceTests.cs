using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlowerDelivery.Application.DTOs;
using FlowerDelivery.Application.Services;
using FlowerDelivery.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace FlowerDelivery.Tests;

[TestFixture]
public class AuthServiceTests
{
    private ApplicationDbContext _context;
    private AuthService _authService;
    private IConfiguration _configuration;

    [OneTimeSetUp]
    public void GlobalSetup()
    {
        // Читаем конфигурацию из файла
        _configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.test.json", optional: false, reloadOnChange: true)
            .Build();

        var connectionString = _configuration.GetConnectionString("TestConnection");

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        _context = new ApplicationDbContext(options);

        _authService = new AuthService(_context, _configuration);
    }

    [SetUp]
    public async Task Setup()
    {
        // Очищаем таблицы в правильном порядке (сначала зависимые, потом основные)
        _context.Deliveries.RemoveRange(_context.Deliveries);
        _context.OrderItems.RemoveRange(_context.OrderItems);
        _context.Orders.RemoveRange(_context.Orders);
        _context.Bouquets.RemoveRange(_context.Bouquets);
        _context.Users.RemoveRange(_context.Users);
        await _context.SaveChangesAsync();
    }

    [TearDown]
    public void TearDown()
    {
        // Ничего не делаем
    }

    [OneTimeTearDown]
    public void GlobalTearDown()
    {
        _context.Dispose();
    }

    [Test]
    public async Task RegisterAsync_ValidClientData_ReturnsAuthResponse()
    {
        var dto = new RegisterDto
        {
            LastName = "Петрова",
            FirstName = "Анна",
            Email = $"anna_{Guid.NewGuid().ToString().Substring(0, 8)}@test.com",
            Password = "123456",
            Role = "Client",
            Phone = "+79001112233"
        };

        var result = await _authService.RegisterAsync(dto);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Email, Is.EqualTo(dto.Email));
        Assert.That(result.Role, Is.EqualTo("Client"));
        Assert.That(result.Token, Is.Not.Null);
        Assert.That(result.Token, Is.Not.Empty);
    }

    [Test]
    public async Task RegisterAsync_ValidCourierData_ReturnsCourierRole()
    {
        var dto = new RegisterDto
        {
            LastName = "Иванов",
            FirstName = "Иван",
            Email = $"ivan_{Guid.NewGuid().ToString().Substring(0, 8)}@test.com",
            Password = "123456",
            Role = "Courier",
            Phone = "+79004445566"
        };

        var result = await _authService.RegisterAsync(dto);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Role, Is.EqualTo("Courier"));
    }

    [Test]
    public void RegisterAsync_DuplicateEmail_ThrowsException()
    {
        var dto = new RegisterDto
        {
            LastName = "Петрова",
            FirstName = "Анна",
            Email = $"duplicate_{Guid.NewGuid().ToString().Substring(0, 8)}@test.com",
            Password = "123456",
            Role = "Client"
        };

        _authService.RegisterAsync(dto).Wait();

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _authService.RegisterAsync(dto));
    }

    [Test]
    public async Task LoginAsync_ValidCredentials_ReturnsAuthResponse()
    {
        var registerDto = new RegisterDto
        {
            LastName = "Тестов",
            FirstName = "Тест",
            Email = $"login_{Guid.NewGuid().ToString().Substring(0, 8)}@test.com",
            Password = "password123",
            Role = "Client"
        };

        await _authService.RegisterAsync(registerDto);

        var loginDto = new LoginDto
        {
            Email = registerDto.Email,
            Password = "password123"
        };

        var result = await _authService.LoginAsync(loginDto);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Email, Is.EqualTo(registerDto.Email));
        Assert.That(result.Token, Is.Not.Null);
    }

    [Test]
    public async Task LoginAsync_InvalidPassword_ReturnsNull()
    {
        var registerDto = new RegisterDto
        {
            LastName = "Тестов",
            FirstName = "Тест",
            Email = $"wrong_{Guid.NewGuid().ToString().Substring(0, 8)}@test.com",
            Password = "correctpassword",
            Role = "Client"
        };

        await _authService.RegisterAsync(registerDto);

        var loginDto = new LoginDto
        {
            Email = registerDto.Email,
            Password = "wrongpassword"
        };

        var result = await _authService.LoginAsync(loginDto);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task LoginAsync_NonExistentEmail_ReturnsNull()
    {
        var loginDto = new LoginDto
        {
            Email = $"nonexistent_{Guid.NewGuid().ToString().Substring(0, 8)}@test.com",
            Password = "password123"
        };

        var result = await _authService.LoginAsync(loginDto);

        Assert.That(result, Is.Null);
    }
}