using System.ComponentModel.DataAnnotations;

namespace FlowerDelivery.Application.DTOs;

public class CreateBouquetDto
{
    [Required(ErrorMessage = "Название букета обязательно")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Название должно быть от 2 до 100 символов")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Описание обязательно")]
    [StringLength(500, MinimumLength = 10, ErrorMessage = "Описание должно быть от 10 до 500 символов")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Цена обязательна")]
    [Range(1, 100000, ErrorMessage = "Цена должна быть от 1 до 100000")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Количество на складе обязательно")]
    [Range(0, 10000, ErrorMessage = "Количество на складе должно быть от 0 до 10000")]
    public int StockQuantity { get; set; }

    [StringLength(500, ErrorMessage = "Путь к изображению не должен превышать 500 символов")]
    public string ImagePath { get; set; } = string.Empty;
}