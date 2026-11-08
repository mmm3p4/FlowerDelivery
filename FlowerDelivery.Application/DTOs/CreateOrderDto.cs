using System.ComponentModel.DataAnnotations;

namespace FlowerDelivery.Application.DTOs;

public class CreateOrderDto
{
    [Required(ErrorMessage = "ID клиента обязателен")]
    [Range(1, int.MaxValue, ErrorMessage = "ID клиента должен быть больше 0")]
    public int ClientId { get; set; }

    [Required(ErrorMessage = "Имя получателя обязательно")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Имя получателя должно быть от 2 до 100 символов")]
    public string RecipientName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Телефон получателя обязателен")]
    [RegularExpression(@"^\+7\d{10}$", ErrorMessage = "Телефон должен быть в формате +7XXXXXXXXXX")]
    public string RecipientPhone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Адрес доставки обязателен")]
    [StringLength(200, MinimumLength = 5, ErrorMessage = "Адрес должен быть от 5 до 200 символов")]
    public string DeliveryAddress { get; set; } = string.Empty;

    [Required(ErrorMessage = "Время доставки обязательно")]
    public DateTime DeliveryTime { get; set; }

    [Required(ErrorMessage = "Список позиций заказа не может быть пустым")]
    [MinLength(1, ErrorMessage = "Должна быть хотя бы одна позиция в заказе")]
    public List<OrderItemDto> Items { get; set; } = new();
}

public class OrderItemDto
{
    [Required(ErrorMessage = "ID букета обязателен")]
    [Range(1, int.MaxValue, ErrorMessage = "ID букета должен быть больше 0")]
    public int BouquetId { get; set; }

    [Required(ErrorMessage = "Количество обязательно")]
    [Range(1, 100, ErrorMessage = "Количество должно быть от 1 до 100")]
    public int Quantity { get; set; }
}