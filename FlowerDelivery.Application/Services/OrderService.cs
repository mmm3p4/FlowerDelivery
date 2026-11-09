using Microsoft.EntityFrameworkCore;
using FlowerDelivery.Domain.Entities;
using FlowerDelivery.Infrastructure;
using FlowerDelivery.Application.DTOs;

namespace FlowerDelivery.Application.Services;

public class OrderService(ApplicationDbContext context)
{
    private readonly ApplicationDbContext _context = context;

    public async Task<OrderResponseDto> CreateOrderAsync(CreateOrderDto dto)
    {
        if (dto.DeliveryTime <= DateTime.UtcNow)
        {
            throw new InvalidOperationException("Время доставки должно быть в будущем");
        }

        var client = await _context.Clients.FindAsync(dto.ClientId) ?? throw new InvalidOperationException($"Клиент с ID {dto.ClientId} не найден");
        var bouquetIds = dto.Items.Select(i => i.BouquetId).Distinct().ToList();
        var bouquets = await _context.Bouquets
            .Where(b => bouquetIds.Contains(b.Id))
            .ToListAsync();

        if (bouquets.Count != bouquetIds.Count)
        {
            var foundIds = bouquets.Select(b => b.Id).ToList();
            var missingIds = bouquetIds.Where(id => !foundIds.Contains(id)).ToList();
            throw new InvalidOperationException($"Букеты с ID {string.Join(", ", missingIds)} не найдены");
        }

        foreach (var item in dto.Items)
        {
            var bouquet = bouquets.First(b => b.Id == item.BouquetId);
            if (bouquet.StockQuantity < item.Quantity)
            {
                throw new InvalidOperationException(
                    $"Недостаточно букета '{bouquet.Name}' на складе. " +
                    $"Доступно: {bouquet.StockQuantity}, запрошено: {item.Quantity}");
            }
        }

        var order = new Order
        {
            ClientId = dto.ClientId,
            RecipientName = dto.RecipientName,
            RecipientPhone = dto.RecipientPhone,
            Status = OrderStatus.Created,
            CreatedAt = DateTime.UtcNow,
            Items = []
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
            Order = order
        };

        _context.Deliveries.Add(delivery);
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return await GetOrderByIdAsync(order.Id)
    ?? throw new InvalidOperationException($"Заказ с ID {order.Id} не найден после создания");
    }

    public async Task<OrderResponseDto?> GetOrderByIdAsync(int id)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Bouquet)
            .Include(o => o.Delivery)
                .ThenInclude(d => d!.Courier)
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

        return [.. orders.Select(MapToResponseDto)];
    }

    public async Task<OrderResponseDto?> UpdateOrderAsync(int id, UpdateOrderDto dto)
    {
        var order = await _context.Orders
            .Include(o => o.Delivery)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return null;


        if (order.Status == OrderStatus.InDelivery ||
            order.Status == OrderStatus.Delivered)
        {
            throw new Exception("Нельзя изменить заказ, который уже передан в доставку или доставлен");
        }

        if (dto.RecipientName != null)
            order.RecipientName = dto.RecipientName;

        if (dto.RecipientPhone != null)
            order.RecipientPhone = dto.RecipientPhone;

        if (order.Delivery != null)
        {
            if (dto.DeliveryAddress != null)
                order.Delivery.Address = dto.DeliveryAddress;

            if (dto.DeliveryTime.HasValue)
                order.Delivery.DeliveryTime = DateTime.SpecifyKind(dto.DeliveryTime.Value, DateTimeKind.Utc);
        }

        await _context.SaveChangesAsync();

        return await GetOrderByIdAsync(id);
    }

    public async Task<OrderResponseDto?> PayOrderAsync(int id)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return null;

        if (order.Status != OrderStatus.Created)
        {
            throw new InvalidOperationException($"Нельзя оплатить заказ со статусом {order.Status}. Заказ должен быть в статусе Created");
        }

        order.Status = OrderStatus.Paid;
        await _context.SaveChangesAsync();

        return await GetOrderByIdAsync(id);
    }

    public async Task<OrderResponseDto?> PrepareOrderAsync(int id)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return null;


        if (order.Status != OrderStatus.Paid)
        {
            throw new InvalidOperationException($"Нельзя подготовить заказ со статусом {order.Status}. Заказ должен быть в статусе Paid");
        }

        order.Status = OrderStatus.ReadyForDelivery;
        await _context.SaveChangesAsync();

        return await GetOrderByIdAsync(id);
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
            Items = [.. order.Items.Select(i => new OrderItemResponseDto
            {
                BouquetId = i.BouquetId,
                BouquetName = i.Bouquet.Name,
                Quantity = i.Quantity,
                Price = i.Price
            })],
            Delivery = order.Delivery != null ? new DeliveryResponseDto
            {
                Id = order.Delivery.Id,
                Address = order.Delivery.Address,
                DeliveryTime = order.Delivery.DeliveryTime,
                CourierId = order.Delivery.CourierId
            } : null
        };
    }
}