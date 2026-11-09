using FlowerDelivery.Domain.Entities;
using FlowerDelivery.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FlowerDelivery.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Все методы требуют авторизации
public class DeliveriesController(ApplicationDbContext context) : ControllerBase
{
    private readonly ApplicationDbContext _context = context;

    // Вспомогательные методы для проверки прав
    private int GetCurrentUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
    private bool IsAdmin() => User.IsInRole("Admin");

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<Delivery>>> GetAllDeliveries()
    {
        var deliveries = await _context.Deliveries
            .Include(d => d.Order)
            .Include(d => d.Courier)
            .ToListAsync();

        return Ok(deliveries);
    }

    // Курьер запрашивает ТОЛЬКО свои доставки (ID берется из токена, а не из URL)
    [HttpGet("my")]
    [Authorize(Roles = "Courier")]
    public async Task<ActionResult<List<Delivery>>> GetMyDeliveries()
    {
        var courierId = GetCurrentUserId();

        var deliveries = await _context.Deliveries
            .Where(d => d.CourierId == courierId)
            .Include(d => d.Order)
            .OrderByDescending(d => d.DeliveryTime)
            .ToListAsync();

        return Ok(deliveries);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Delivery>> GetDelivery(int id)
    {
        var delivery = await _context.Deliveries
            .Include(d => d.Order)
            .Include(d => d.Courier)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (delivery == null)
        {
            return NotFound(new { message = "Доставка не найдена" });
        }

        // Проверка прав: Админ видит всё, остальные — только если они курьер этой доставки или клиент этого заказа
        if (!IsAdmin())
        {
            var userId = GetCurrentUserId();
            bool isAssignedCourier = delivery.CourierId == userId;
            bool isOrderClient = delivery.Order?.ClientId == userId;

            if (!isAssignedCourier && !isOrderClient)
            {
                return Forbid();
            }
        }

        return Ok(delivery);
    }

    [HttpPut("{id}/assign-courier")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AssignCourier(int id, [FromBody] AssignCourierDto dto)
    {
        var delivery = await _context.Deliveries
            .Include(d => d.Order)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (delivery == null)
        {
            return NotFound(new { message = "Доставка не найдена" });
        }

        var courier = await _context.Couriers.FindAsync(dto.CourierId);
        if (courier == null)
        {
            return BadRequest(new { message = "Курьер не найден" });
        }

        if (courier.Status != CourierStatus.Free)
        {
            return BadRequest(new { message = "Курьер уже занят другой доставкой" });
        }

        delivery.CourierId = dto.CourierId;
        courier.Status = CourierStatus.Busy;

        if (delivery.Order != null)
        {
            delivery.Order.Status = OrderStatus.InDelivery;
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "Курьер успешно назначен" });
    }

    [HttpPut("{id}/confirm")]
    [Authorize(Roles = "Courier")]
    public async Task<IActionResult> ConfirmDelivery(int id)
    {
        var delivery = await _context.Deliveries
            .Include(d => d.Order)
            .Include(d => d.Courier)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (delivery == null)
        {
            return NotFound(new { message = "Доставка не найдена" });
        }

        // Курьер может подтвердить ТОЛЬКО свою доставку
        if (delivery.CourierId != GetCurrentUserId())
        {
            return Forbid();
        }

        if (delivery.Order != null)
        {
            delivery.Order.Status = OrderStatus.Delivered;
        }

        if (delivery.Courier != null)
        {
            delivery.Courier.Status = CourierStatus.Free;
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "Доставка подтверждена" });
    }

    [HttpPut("{id}/return")]
    [Authorize(Roles = "Courier,Admin")]
    public async Task<IActionResult> ReturnDelivery(int id)
    {
        var delivery = await _context.Deliveries
            .Include(d => d.Order)
            .Include(d => d.Courier)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (delivery == null)
        {
            return NotFound(new { message = "Доставка не найдена" });
        }

        // Курьер может вернуть ТОЛЬКО свою доставку
        if (!IsAdmin() && delivery.CourierId != GetCurrentUserId())
        {
            return Forbid();
        }

        if (delivery.Order != null)
        {
            delivery.Order.Status = OrderStatus.Returned;
        }

        if (delivery.Courier != null)
        {
            delivery.Courier.Status = CourierStatus.Free;
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "Заказ возвращён" });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteDelivery(int id)
    {
        var delivery = await _context.Deliveries
            .Include(d => d.Order)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (delivery == null)
        {
            return NotFound(new { message = "Доставка не найдена" });
        }

        if (delivery.Order != null &&
            (delivery.Order.Status == OrderStatus.InDelivery ||
             delivery.Order.Status == OrderStatus.Delivered))
        {
            return BadRequest(new { message = "Нельзя удалить доставку, если заказ уже в пути или доставлен" });
        }

        _context.Deliveries.Remove(delivery);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

// DTO можно оставить здесь или перенести в папку FlowerDelivery.Application.DTOs
public class AssignCourierDto
{
    public int CourierId { get; set; }
}