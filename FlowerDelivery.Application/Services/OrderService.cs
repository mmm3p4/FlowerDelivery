using Microsoft.EntityFrameworkCore;
using FlowerDelivery.Domain.Entities;
using FlowerDelivery.Infrastructure.Data;
using FlowerDelivery.Application.DTOs;

namespace FlowerDelivery.Application.Services;

public class OrderService
{
    private readonly ApplicationDbContext _context;

    public OrderService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<OrderResponseDto> CreateOrderAsync(CreateOrderDto dto)
    {
        var bouquetIds = dto.Items.Select(i => i.BouquetId).ToList();
        var bouquets = await _context.Bouquets
            .Where(b => bouquetIds.Contains(b.Id))
            .ToListAsync();

        if (bouquets.Count != bouquetIds.Count)
        {
            throw new Exception("Один или несколько букетов не найдены");
        }


        foreach (var item in dto.Items)
        {
            var bouquet = bouquets.First(b => b.Id == item.BouquetId);
            if (bouquet.StockQuantity < item.Quantity)
            {
                throw new Exception($"Букет '{bouquet.Name}' недоступен в количестве {item.Quantity}. Остаток: {bouquet.StockQuantity}");
            }
        }

        var order = new Order
        {
            ClientId = dto.ClientId,
            RecipientName = dto.RecipientName,
            RecipientPhone = dto.RecipientPhone,
            Status = OrderStatus.Created,
            CreatedAt = DateTime.UtcNow,
            Items = new List<OrderItem>()
        };

        decimal totalAmount = 0;
        foreach (var item in dto.Items)
        {
            var bouquet = bouquets.First(b => b.Id == item.BouquetId);
            var orderItem = new OrderItem
            {
                BouquetId = bouquet.Id,
                Quantity = item.Quantity,
                Price = bouquet.Price * item.Quantity
            };
            order.Items.Add(orderItem);
            totalAmount += orderItem.Price;

            bouquet.StockQuantity -= item.Quantity;
        }

        order.TotalAmount = totalAmount;

        var delivery = new Delivery
        {
            Address = dto.DeliveryAddress,
            DeliveryTime = DateTime.SpecifyKind(dto.DeliveryTime, DateTimeKind.Utc),
            Status = DeliveryStatus.Assigned,
            Order = order
        };

        order.Delivery = delivery;

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return await GetOrderByIdAsync(order.Id);
    }

    public async Task<OrderResponseDto?> GetOrderByIdAsync(int id)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Bouquet)
            .Include(o => o.Delivery)
                .ThenInclude(d => d.Courier)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return null;

        return MapToResponseDto(order);
    }

    public async Task<List<OrderResponseDto>> GetOrdersByClientIdAsync(int clientId)
    {
        var orders = await _context.Orders
            .Where(o => o.ClientId == clientId)
            .Include(o => o.Items)
                .ThenInclude(i => i.Bouquet)
            .Include(o => o.Delivery)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(MapToResponseDto).ToList();
    }

    private OrderResponseDto MapToResponseDto(Order order)
    {
        return new OrderResponseDto
        {
            Id = order.Id,
            CreatedAt = order.CreatedAt,
            Status = order.Status.ToString(),
            TotalAmount = order.TotalAmount,
            RecipientName = order.RecipientName,
            RecipientPhone = order.RecipientPhone,
            ClientId = order.ClientId,
            Items = order.Items.Select(i => new OrderItemResponseDto
            {
                BouquetId = i.BouquetId,
                BouquetName = i.Bouquet.Name,
                Quantity = i.Quantity,
                Price = i.Price
            }).ToList(),
            Delivery = order.Delivery != null ? new DeliveryResponseDto
            {
                Id = order.Delivery.Id,
                Address = order.Delivery.Address,
                DeliveryTime = order.Delivery.DeliveryTime,
                Status = order.Delivery.Status.ToString(),
                CourierId = order.Delivery.CourierId
            } : null
        };
    }
}