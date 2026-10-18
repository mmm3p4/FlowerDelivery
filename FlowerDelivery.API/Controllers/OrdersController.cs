using Microsoft.AspNetCore.Mvc;
using FlowerDelivery.Application.Services;
using FlowerDelivery.Application.DTOs;

namespace FlowerDelivery.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly OrderService _orderService;

    public OrdersController(OrderService orderService)
    {
        _orderService = orderService;
    }

    // POST: api/orders
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

    // GET: api/orders/5
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

    // GET: api/orders/client/5
    [HttpGet("client/{clientId}")]
    public async Task<ActionResult<List<OrderResponseDto>>> GetOrdersByClient(int clientId)
    {
        var orders = await _orderService.GetOrdersByClientIdAsync(clientId);
        return Ok(orders);
    }
}