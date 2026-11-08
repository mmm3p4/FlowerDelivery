using FlowerDelivery.Application.DTOs;
using FlowerDelivery.Application.Services;
using FlowerDelivery.Domain.Entities;
using FlowerDelivery.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlowerDelivery.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly OrderService _orderService;
    private readonly ApplicationDbContext _context;

    public OrdersController(OrderService orderService, ApplicationDbContext context)
    {
        _orderService = orderService;
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<OrderResponseDto>>> GetAllOrders()
    {
        var orders = await _context.Orders
            .Include(o => o.Items).ThenInclude(i => i.Bouquet)
            .Include(o => o.Delivery)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return Ok(orders);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderResponseDto>> GetOrder(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(id);

        if (order == null)
        {
            return NotFound();
        }

        return order;
    }

    [HttpGet("client/{clientId}")]
    public async Task<ActionResult<List<OrderResponseDto>>> GetOrdersByClient(int clientId)
    {
        var orders = await _orderService.GetOrdersByClientIdAsync(clientId);
        return Ok(orders);
    }

    [HttpPost]
    public async Task<ActionResult<OrderResponseDto>> CreateOrder([FromBody] CreateOrderDto dto)
    {
        try
        {
            var order = await _orderService.CreateOrderAsync(dto);
            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateOrder(int id, [FromBody] UpdateOrderDto dto)
    {
        try
        {
            var order = await _orderService.UpdateOrderAsync(id, dto);

            if (order == null)
            {
                return NotFound(new { message = "Заказ не найден" });
            }

            return Ok(order);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }


    [HttpDelete("{id}")]
    public async Task<IActionResult> CancelOrder(int id)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .Include(o => o.Delivery)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFound(new { message = "Заказ не найден" });
        }

        if (order.Status == OrderStatus.InDelivery ||
            order.Status == OrderStatus.Delivered)
        {
            return BadRequest(new { message = "Нельзя отменить заказ, который уже в доставке или доставлен" });
        }

        foreach (var item in order.Items)
        {
            var bouquet = await _context.Bouquets.FindAsync(item.BouquetId);
            if (bouquet != null)
            {
                bouquet.StockQuantity += item.Quantity;
            }
        }

        order.Status = OrderStatus.Cancelled;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Заказ отменён", orderId = id });
    }
    // POST: api/orders/5/pay — Оплата заказа
    [HttpPost("{id}/pay")]
    public async Task<IActionResult> PayOrder(int id)
    {
        try
        {
            var order = await _orderService.PayOrderAsync(id);

            if (order == null)
            {
                return NotFound(new { message = "Заказ не найден" });
            }

            return Ok(new { message = "Заказ оплачен", order });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // PUT: api/orders/5/prepare — Подготовка заказа флористом
    [HttpPut("{id}/prepare")]
    public async Task<IActionResult> PrepareOrder(int id)
    {
        try
        {
            var order = await _orderService.PrepareOrderAsync(id);

            if (order == null)
            {
                return NotFound(new { message = "Заказ не найден" });
            }

            return Ok(new { message = "Заказ готов к доставке", order });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}