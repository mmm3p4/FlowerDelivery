using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlowerDelivery.Application.DTOs;
using FlowerDelivery.Application.Services;
using FlowerDelivery.Domain.Entities;
using FlowerDelivery.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace FlowerDelivery.Tests;

[TestFixture]
public class OrderServiceTests
{
    private ApplicationDbContext _context;
    private OrderService _orderService;
    private IConfiguration _configuration;

    [OneTimeSetUp]
    public void GlobalSetup()
    {
        _configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.test.json", optional: false, reloadOnChange: true)
            .Build();

        var connectionString = _configuration.GetConnectionString("TestConnection");

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        _context = new ApplicationDbContext(options);
        _orderService = new OrderService(_context);
    }

    [SetUp]
    public async Task Setup()
    {
        // Очищаем таблицы в правильном порядке
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
    public async Task CreateOrderAsync_ValidData_CreatesOrder()
    {
        // Создаём клиента и букет
        var client = new Client
        {
            LastName = "Тестов",
            FirstName = "Клиент",
            Email = $"client_{Guid.NewGuid().ToString().Substring(0, 8)}@test.com",
            PasswordHash = "hashed",
            Role = "Client",
            Phone = "+79001112233"
        };

        var bouquet = new Bouquet
        {
            Name = "Тестовый букет",
            Description = "Описание",
            Price = 1000,
            StockQuantity = 10
        };

        _context.Users.Add(client);
        _context.Bouquets.Add(bouquet);
        await _context.SaveChangesAsync();

        var dto = new CreateOrderDto
        {
            ClientId = client.Id,
            RecipientName = "Получатель",
            RecipientPhone = "+79009998877",
            DeliveryAddress = "ул. Тестовая, 1",
            DeliveryTime = DateTime.UtcNow.AddDays(1),
            Items = new List<OrderItemDto>
            {
                new OrderItemDto { BouquetId = bouquet.Id, Quantity = 2 }
            }
        };

        var result = await _orderService.CreateOrderAsync(dto);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.ClientId, Is.EqualTo(client.Id));
        Assert.That(result.RecipientName, Is.EqualTo("Получатель"));
        Assert.That(result.TotalAmount, Is.EqualTo(2000m));
        Assert.That(result.Delivery, Is.Not.Null);
    }

    [Test]
    public void CreateOrderAsync_InvalidClientId_ThrowsException()
    {
        var dto = new CreateOrderDto
        {
            ClientId = 999,
            RecipientName = "Получатель",
            RecipientPhone = "+79009998877",
            DeliveryAddress = "ул. Тестовая, 1",
            DeliveryTime = DateTime.UtcNow.AddDays(1),
            Items = new List<OrderItemDto>
            {
                new OrderItemDto { BouquetId = 1, Quantity = 1 }
            }
        };

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _orderService.CreateOrderAsync(dto));
    }

    [Test]
    public void CreateOrderAsync_InsufficientStock_ThrowsException()
    {
        var client = new Client
        {
            LastName = "Тестов",
            FirstName = "Клиент",
            Email = $"client2_{Guid.NewGuid().ToString().Substring(0, 8)}@test.com",
            PasswordHash = "hashed",
            Role = "Client",
            Phone = "+79001112233"
        };

        var bouquet = new Bouquet
        {
            Name = "Мало букетов",
            Description = "Описание",
            Price = 1000,
            StockQuantity = 1 // Только 1 штука
        };

        _context.Users.Add(client);
        _context.Bouquets.Add(bouquet);
        _context.SaveChanges();

        var dto = new CreateOrderDto
        {
            ClientId = client.Id,
            RecipientName = "Получатель",
            RecipientPhone = "+79009998877",
            DeliveryAddress = "ул. Тестовая, 1",
            DeliveryTime = DateTime.UtcNow.AddDays(1),
            Items = new List<OrderItemDto>
            {
                new OrderItemDto { BouquetId = bouquet.Id, Quantity = 100 } // Нужно 100, есть только 1
            }
        };

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _orderService.CreateOrderAsync(dto));
    }

    [Test]
    public async Task PayOrderAsync_ValidOrder_ChangesStatusToPaid()
    {
        var client = new Client
        {
            LastName = "Тестов",
            FirstName = "Клиент",
            Email = $"client3_{Guid.NewGuid().ToString().Substring(0, 8)}@test.com",
            PasswordHash = "hashed",
            Role = "Client",
            Phone = "+79001112233"
        };

        _context.Users.Add(client);
        await _context.SaveChangesAsync();

        var order = new Order
        {
            ClientId = client.Id,
            Status = OrderStatus.Created,
            TotalAmount = 1000,
            RecipientName = "Тест",
            RecipientPhone = "+79001112233"
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var result = await _orderService.PayOrderAsync(order.Id);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Status, Is.EqualTo("Paid"));

    }

    [Test]
    public void PayOrderAsync_NonExistentOrder_ReturnsNull()
    {
        var result = _orderService.PayOrderAsync(999).Result;
        Assert.That(result, Is.Null);
    }
}