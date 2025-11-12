using Core.Entities;
using Core.Entities.Enums;
using Infrastructure.Dat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.DTOs;

[ApiController]
[Route("materials")]
public class MaterialsController : ControllerBase
{
    private readonly AppDbContext _db;
    public MaterialsController(AppDbContext db) => _db = db;

    // 🔹 GET /materials
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _db.Materials
            .Select(m => new MaterialDto(m.Id, m.Name, m.Category.ToString(), m.Quantity))
            .ToListAsync());

    // 🔹 GET /materials/by-category/{category}
    [HttpGet("by-category/{category}")]
    public async Task<IActionResult> GetByCategory(string category)
    {
        if (!Enum.TryParse<MaterialCategory>(category, true, out var parsedCategory))
            return BadRequest($"Categoria inválida: {category}");

        var materials = await _db.Materials
            .Where(m => m.Category == parsedCategory)
            .Select(m => new MaterialDto(m.Id, m.Name, m.Category.ToString(), m.Quantity))
            .ToListAsync();

        return Ok(materials);
    }

    // 🔹 GET /materials/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var m = await _db.Materials.FindAsync(id);
        if (m == null) return NotFound();
        return Ok(new MaterialDto(m.Id, m.Name, m.Category.ToString(), m.Quantity));
    }

    // 🔹 POST /materials
    [HttpPost]
    [Authorize(Roles = "Master")]
    public async Task<IActionResult> Create([FromBody] MaterialCreateRequest dto)
    {
        if (!Enum.TryParse<MaterialCategory>(dto.Category, true, out var parsedCategory))
            return BadRequest($"Categoria inválida: {dto.Category}");

        var material = new Material
        {
            Name = dto.Name,
            Category = parsedCategory,
            Quantity = dto.Quantity
        };

        _db.Materials.Add(material);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = material.Id },
            new MaterialDto(material.Id, material.Name, material.Category.ToString(), material.Quantity));
    }

    // 🔹 PUT /materials/{id}
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Master")]
    public async Task<IActionResult> Update(Guid id, [FromBody] MaterialDto dto)
    {
        var m = await _db.Materials.FindAsync(id);
        if (m == null) return NotFound();

        if (!Enum.TryParse<MaterialCategory>(dto.Category, true, out var parsedCategory))
            return BadRequest($"Categoria inválida: {dto.Category}");

        m.Name = dto.Name;
        m.Category = parsedCategory;
        m.Quantity = dto.Quantity;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("batch")]
    [Authorize(Roles = "Master")]
    public async Task<IActionResult> CreateBatch([FromBody] MaterialCreateBatchRequest request)
    {
        foreach (var item in request.Materials)
        {
            var material = new Material
            {
                Name = item.Name,
                Category = Enum.Parse<MaterialCategory>(item.Category),
                Quantity = item.Quantity
            };

            _db.Materials.Add(material);
        }

        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("{id:guid}/add-stock")]
    [Authorize(Roles = "Master")]
    public async Task<IActionResult> AddStock(Guid id, [FromBody] MaterialAdjustQuantityRequest request)
    {
        if (request.Quantity <= 0)
            return BadRequest("Quantidade deve ser maior que zero.");

        var material = await _db.Materials.FindAsync(id);
        if (material == null) return NotFound();

        material.Quantity += request.Quantity;
        await _db.SaveChangesAsync();

        return Ok(new MaterialDto(material.Id, material.Name, material.Category.ToString(), material.Quantity));
    }


    // 🔹 DELETE /materials/{id}
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Master")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var m = await _db.Materials.FindAsync(id);
        if (m == null) return NotFound();

        _db.Materials.Remove(m);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
