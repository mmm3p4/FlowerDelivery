using FlowerDelivery.Application.DTOs;
using FlowerDelivery.Domain.Entities;
using FlowerDelivery.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlowerDelivery.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BouquetsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public BouquetsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Bouquet>>> GetAllBouquets()
    {
        return await _context.Bouquets.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Bouquet>> GetBouquet(int id)
    {
        var bouquet = await _context.Bouquets.FindAsync(id);

        if (bouquet == null)
        {
            return NotFound(new { message = "Букет не найден" });
        }

        return bouquet;
    }

    [HttpPost]
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
    public async Task<IActionResult> UpdateBouquet(int id, [FromBody] CreateBouquetDto dto)
    {
        var bouquet = await _context.Bouquets.FindAsync(id);
        if (bouquet == null) return NotFound();

        bouquet.Name = dto.Name;
        bouquet.Description = dto.Description;
        bouquet.Price = dto.Price;
        bouquet.StockQuantity = dto.StockQuantity;
        bouquet.ImagePath = dto.ImagePath;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBouquet(int id)
    {
        var bouquet = await _context.Bouquets.FindAsync(id);
        if (bouquet == null)
        {
            return NotFound(new { message = "Букет не найден" });
        }

        _context.Bouquets.Remove(bouquet);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool BouquetExists(int id)
    {
        return _context.Bouquets.Any(e => e.Id == id);
    }
}