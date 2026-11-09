using FlowerDelivery.Domain.Entities;
using FlowerDelivery.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlowerDelivery.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BouquetsController(ApplicationDbContext context) : ControllerBase
{
    private readonly ApplicationDbContext _context = context;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Bouquet>>> GetAllBouquets()
    {
        var bouquets = await _context.Bouquets
            .OrderBy(b => b.Name)
            .ToListAsync();

        return Ok(bouquets);
    }


    [HttpGet("{id}")]
    public async Task<ActionResult<Bouquet>> GetBouquet(int id)
    {
        var bouquet = await _context.Bouquets.FindAsync(id);

        if (bouquet == null)
        {
            return NotFound(new { message = "Букет не найден" });
        }

        return Ok(bouquet);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Bouquet>> CreateBouquet([FromBody] CreateBouquetDto dto)
    {
        var bouquet = new Bouquet
        {
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            StockQuantity = dto.StockQuantity,
            ImagePath = dto.ImagePath
        };

        _context.Bouquets.Add(bouquet);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetBouquet), new { id = bouquet.Id }, bouquet);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateBouquet(int id, [FromBody] CreateBouquetDto dto)
    {
        var bouquet = await _context.Bouquets.FindAsync(id);

        if (bouquet == null)
        {
            return NotFound(new { message = "Букет не найден" });
        }

        bouquet.Name = dto.Name;
        bouquet.Description = dto.Description;
        bouquet.Price = dto.Price;
        bouquet.StockQuantity = dto.StockQuantity;
        bouquet.ImagePath = dto.ImagePath;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Букет обновлён", bouquet });
    }


    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteBouquet(int id)
    {
        var bouquet = await _context.Bouquets.FindAsync(id);

        if (bouquet == null)
        {
            return NotFound(new { message = "Букет не найден" });
        }

        var hasActiveOrders = await _context.OrderItems
            .AnyAsync(oi => oi.BouquetId == id);

        if (hasActiveOrders)
        {
            return BadRequest(new { message = "Нельзя удалить букет, который есть в заказах" });
        }

        _context.Bouquets.Remove(bouquet);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

public class CreateBouquetDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string ImagePath { get; set; } = string.Empty;
}