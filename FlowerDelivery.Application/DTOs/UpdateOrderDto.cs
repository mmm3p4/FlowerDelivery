namespace FlowerDelivery.Application.DTOs;

public class UpdateOrderDto
{
    public string? RecipientName { get; set; }
    public string? RecipientPhone { get; set; }
    public string? DeliveryAddress { get; set; }
    public DateTime? DeliveryTime { get; set; }
}