using System.ComponentModel.DataAnnotations;

namespace FlowerDelivery.Application.DTOs;

public class RegisterDto
{
    [Required(ErrorMessage = "Фамилия обязательна")]
    [StringLength(50, MinimumLength = 2)]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Имя обязательно")]
    [StringLength(50, MinimumLength = 2)]
    public string FirstName { get; set; } = string.Empty;

    [StringLength(50)]
    public string? MiddleName { get; set; }

    [Required(ErrorMessage = "Email обязателен")]
    [EmailAddress(ErrorMessage = "Некорректный формат email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Пароль обязателен")]
    [MinLength(6, ErrorMessage = "Пароль должен содержать минимум 6 символов")]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Роль обязательна")]
    [RegularExpression("^(Client|Courier|Admin)$", ErrorMessage = "Роль должна быть Client, Courier или Admin")]
    public string Role { get; set; } = "Client";
}