namespace FlowerDelivery.Domain.Entities;

public class Bouquet
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Description { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public string ImagePath { get; set; } = string.Empty;

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}