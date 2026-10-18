namespace FlowerDelivery.Application.DTOs;

public class CreateOrderDto
{
    public int ClientId { get; set; }
    public string RecipientName { get; set; } = string.Empty;
    public string RecipientPhone { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public DateTime DeliveryTime { get; set; }

    public List<OrderItemDto> Items { get; set; } = new();
}

public class OrderItemDto
{
    public int BouquetId { get; set; }
    public int Quantity { get; set; }
}