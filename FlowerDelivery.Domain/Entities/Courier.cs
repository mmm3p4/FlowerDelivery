namespace FlowerDelivery.Domain.Entities;

public class Courier : User
{
    public string Phone { get; set; } = string.Empty;
    public CourierStatus Status { get; set; } = CourierStatus.Free;

    public ICollection<Delivery> Deliveries { get; set; } = new List<Delivery>();
}

public enum CourierStatus
{
    Free,
    Busy
}