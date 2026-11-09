using FlowerDelivery.Application.DTOs;
using FlowerDelivery.Application.Services;
using FlowerDelivery.Domain.Entities;
using FlowerDelivery.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FlowerDelivery.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController(OrderService orderService, ApplicationDbContext context) : ControllerBase
{
    private readonly OrderService _orderService = orderService;
    private readonly ApplicationDbContext _context = context;

    private int GetCurrentUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
    private bool IsAdmin() => User.IsInRole("Admin");

    [HttpGet]
    public async Task<ActionResult<List<OrderResponseDto>>> GetAllOrders()
    {
        if (IsAdmin())
        {
            var orders = await _context.Orders
                .Include(o => o.Items).ThenInclude(i => i.Bouquet)
                .Include(o => o.Delivery)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();


            return Ok(orders);
        }

        var clientId = GetCurrentUserId();
        var clientOrders = await _orderService.GetOrdersByClientIdAsync(clientId);
        return Ok(clientOrders);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderResponseDto>> GetOrder(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(id);

        if (order == null)
        {
            return NotFound(new { message = "Заказ не найден" });
        }

        if (!IsAdmin() && order.ClientId != GetCurrentUserId())
        {
            return Forbid();
        }

        return Ok(order);
    }

    [HttpGet("client/{clientId}")]
    public async Task<ActionResult<List<OrderResponseDto>>> GetOrdersByClient(int clientId)
    {
        if (!IsAdmin() && clientId != GetCurrentUserId())
        {
            return Forbid();
        }

        var orders = await _orderService.GetOrdersByClientIdAsync(clientId);
        return Ok(orders);
    }

    [HttpPost]
    [Authorize(Roles = "Client")]
    public async Task<ActionResult<OrderResponseDto>> CreateOrder([FromBody] CreateOrderDto dto)
    {
        var userId = GetCurrentUserId();

        if (dto.ClientId != userId)
        {
            return StatusCode(403, new { message = "Нельзя создать заказ от имени другого пользователя" });
        }

        var order = await _orderService.CreateOrderAsync(dto);

        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
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

        if (!IsAdmin() && order.ClientId != GetCurrentUserId())
        {
            return StatusCode(403,new { message = "Нельзя отменить чужой заказ" });
        }

        if (order.Status == OrderStatus.InDelivery || order.Status == OrderStatus.Delivered)
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

    [HttpPost("{id}/pay")]
    [Authorize(Roles = "Client,Admin")]
    public async Task<IActionResult> PayOrder(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(id);
        if (order == null) return NotFound(new { message = "Заказ не найден" });

        if (!IsAdmin() && order.ClientId != GetCurrentUserId())
        {
            return StatusCode(403, new { message = "Нельзя оплатить чужой заказ" });
        }

        try
        {
            var paidOrder = await _orderService.PayOrderAsync(id);
            return Ok(new { message = "Заказ оплачен", order = paidOrder });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}/prepare")]
    [Authorize(Roles = "Admin")]
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