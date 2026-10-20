using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FlowerDelivery.Infrastructure.Data;
using FlowerDelivery.Domain.Entities;

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
    public async Task<ActionResult<Bouquet>> CreateBouquet(Bouquet bouquet)
    {
        _context.Bouquets.Add(bouquet);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetBouquet), new { id = bouquet.Id }, bouquet);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBouquet(int id, Bouquet bouquet)
    {
        if (id != bouquet.Id)
        {
            return BadRequest(new { message = "ID букета не совпадает" });
        }

        _context.Entry(bouquet).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!BouquetExists(id))
            {
                return NotFound(new { message = "Букет не найден" });
            }
            else
            {
                throw;
            }
        }

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