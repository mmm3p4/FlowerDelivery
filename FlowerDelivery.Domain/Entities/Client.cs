namespace FlowerDelivery.Domain.Entities;

public class Client : User
{
    public string Phone { get; set; } = string.Empty;
    public string DefaultAddress { get; set; } = string.Empty;

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}