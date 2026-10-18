using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FlowerDelivery.Infrastructure.Data;
using FlowerDelivery.Domain.Entities;

namespace FlowerDelivery.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DeliveriesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public DeliveriesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/deliveries
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Delivery>>> GetAllDeliveries()
    {
        var deliveries = await _context.Deliveries
            .Include(d => d.Order)
            .Include(d => d.Courier)
            .ToListAsync();

        return Ok(deliveries);
    }

    // GET: api/deliveries/courier/5
    [HttpGet("courier/{courierId}")]
    public async Task<ActionResult<List<Delivery>>> GetDeliveriesByCourier(int courierId)
    {
        var deliveries = await _context.Deliveries
            .Where(d => d.CourierId == courierId)
            .Include(d => d.Order)
            .ToListAsync();

        return Ok(deliveries);
    }

    // PUT: api/deliveries/5/assign-courier
    [HttpPut("{id}/assign-courier")]
    public async Task<IActionResult> AssignCourier(int id, [FromBody] AssignCourierDto dto)
    {
        var delivery = await _context.Deliveries
            .Include(d => d.Order)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (delivery == null)
        {
            return NotFound();
        }

        var courier = await _context.Couriers.FindAsync(dto.CourierId);
        if (courier == null || courier.Status != CourierStatus.Free)
        {
            return BadRequest(new { message = "Курьер не найден или уже занят" });
        }

        delivery.CourierId = dto.CourierId;
        delivery.Status = DeliveryStatus.InProgress;
        courier.Status = CourierStatus.Busy;

        if (delivery.Order != null)
        {
            delivery.Order.Status = OrderStatus.InDelivery;
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "Курьер успешно назначен" });
    }

    // PUT: api/deliveries/5/confirm
    [HttpPut("{id}/confirm")]
    public async Task<IActionResult> ConfirmDelivery(int id)
    {
        var delivery = await _context.Deliveries
            .Include(d => d.Order)
            .Include(d => d.Courier)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (delivery == null)
        {
            return NotFound();
        }

        delivery.Status = DeliveryStatus.Completed;

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

    // PUT: api/deliveries/5/return
    [HttpPut("{id}/return")]
    public async Task<IActionResult> ReturnDelivery(int id)
    {
        var delivery = await _context.Deliveries
            .Include(d => d.Order)
            .Include(d => d.Courier)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (delivery == null)
        {
            return NotFound();
        }

        delivery.Status = DeliveryStatus.Returned;

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
}

public class AssignCourierDto
{
    public int CourierId { get; set; }
}