using Core.Entities;
using Core.Entities.Enums;
using Infrastructure.Dat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.DTOs;

[ApiController]
[Route("materials")]
public class MaterialsController(AppDbContext db) : ControllerBase
{
    private readonly AppDbContext _db = db;

    // 🔹 GET /materials
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool withOpenStatus = false)
    {
        if (withOpenStatus)
        {
            var materials = await _db.Materials.ToListAsync();
            var allClinicStocks = await _db.ClinicStocks
                .Include(cs => cs.Clinic)
                .Include(cs => cs.Material)
                .ToListAsync();

            var materiaisAbertosStocks = allClinicStocks
                .Where(cs => cs.Material.Category == Core.Entities.Enums.MaterialCategory.UsageMaterials ||
                            cs.Material.Category == Core.Entities.Enums.MaterialCategory.Disposables)
                .ToList();

            var result = materials.Select(m => 
            {
                var distributedQuantity = allClinicStocks
                    .Where(cs => cs.MaterialId == m.Id)
                    .Sum(cs => cs.QuantityAvailable);

                return new MaterialWithOpenStatusDto(
                    m.Id,
                    m.Name,
                    m.Category.ToString(),
                    m.Quantity,
                    distributedQuantity,
                    materiaisAbertosStocks
                        .Where(cs => cs.MaterialId == m.Id)
                        .Select(cs => new ClinicOpenStatusDto(
                            cs.ClinicId,
                            cs.Clinic.Name,
                            cs.IsOpen,
                            cs.OpenedAt))
                        .ToList()
                );
            }).ToList();

            return Ok(result);
        }
        else
        {
            return Ok(await _db.Materials
                .Select(m => new MaterialDto(m.Id, m.Name, m.Category.ToString(), m.Quantity))
                .ToListAsync());
        }
    }

    // 🔹 GET /materials/summary
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var materials = await _db.Materials
            .AsNoTracking()
            .OrderBy(m => m.Name)
            .ToListAsync();

        var clinics = await _db.Clinics
            .AsNoTracking()
            .Select(c => new { c.Id, c.Name })
            .ToListAsync();
        var clinicNameLookup = clinics.ToDictionary(c => c.Id, c => c.Name);

        var clinicStocks = await _db.ClinicStocks
            .AsNoTracking()
            .Select(cs => new
            {
                cs.MaterialId,
                cs.ClinicId,
                cs.QuantityAvailable
            })
            .ToListAsync();

        var result = materials.Select(material =>
        {
            var clinicsWithMaterial = clinicStocks
                .Where(cs => cs.MaterialId == material.Id)
                .GroupBy(cs => cs.ClinicId)
                .Select(group =>
                {
                    clinicNameLookup.TryGetValue(group.Key, out var clinicName);
                    return new MaterialClinicStockDto(
                        group.Key,
                        clinicName ?? "Clínica desconhecida",
                        group.Sum(x => x.QuantityAvailable));
                })
                .Where(dto => dto.Quantity > 0)
                .OrderByDescending(dto => dto.Quantity)
                .ToList();

            var totalQuantity = clinicsWithMaterial.Sum(c => c.Quantity);

            return new MaterialGeneralStockDto(
                material.Id,
                material.Name,
                material.Category.ToString(),
                material.Quantity,
                totalQuantity,
                clinicsWithMaterial);
        }).ToList();

        return Ok(result);
    }

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
    [Authorize(Roles = "Master, User")]
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
    [Authorize(Roles = "Master, User")]
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
            return BadRequest("Quantity must be greater than zero.");

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
