namespace FlowerDelivery.Domain.Entities;

public class Delivery
{
    public int Id { get; set; }
    public string Address { get; set; } = string.Empty;
    public DateTime DeliveryTime { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public int? CourierId { get; set; }
    public Courier? Courier { get; set; }
}
