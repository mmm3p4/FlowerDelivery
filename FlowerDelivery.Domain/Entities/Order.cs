namespace FlowerDelivery.Domain.Entities;

public class Order
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public OrderStatus Status { get; set; } = OrderStatus.Created;
    public decimal TotalAmount { get; set; }
    public string RecipientName { get; set; } = string.Empty;
    public string RecipientPhone { get; set; } = string.Empty;

    public int ClientId { get; set; }
    public Client Client { get; set; } = null!;

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public Delivery? Delivery { get; set; }
}

public enum OrderStatus
{
    Created,
    Paid,
    ReadyForDelivery,
    InDelivery,
    Delivered,
    Cancelled,
    Returned
}