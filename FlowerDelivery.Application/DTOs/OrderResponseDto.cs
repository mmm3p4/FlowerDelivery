namespace FlowerDelivery.Application.DTOs;

public class OrderResponseDto
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string RecipientName { get; set; } = string.Empty;
    public string RecipientPhone { get; set; } = string.Empty;
    public int ClientId { get; set; }
    public List<OrderItemResponseDto> Items { get; set; } = new();
    public DeliveryResponseDto? Delivery { get; set; }
}

public class OrderItemResponseDto
{
    public int BouquetId { get; set; }
    public string BouquetName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public class DeliveryResponseDto
{
    public int Id { get; set; }
    public string Address { get; set; } = string.Empty;
    public DateTime DeliveryTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? CourierId { get; set; }
}