using Core.Entities;
using Core.Entities.Enums;
using Infrastructure.Dat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.DTOs;
using System.Security.Claims;

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
                .Select(m => new MaterialDto(m.Id, m.Name, m.Category.ToString(), m.Quantity, m.Cost, m.CreatedAt, m.LastAddedQuantity, m.LastAddedTotal))
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
                //.Where(dto => dto.Quantity > 0) // Allow zero quantity items to be listed
                .OrderByDescending(dto => dto.Quantity)
                .ToList();

            var totalQuantity = clinicsWithMaterial.Sum(c => c.Quantity);

            return new MaterialGeneralStockDto(
                material.Id,
                material.Name,
                material.Category.ToString(),
                material.Quantity,
                totalQuantity,
                material.Cost,
                material.CreatedAt,
                material.LastAddedQuantity,
                material.LastAddedTotal,
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
            .Select(m => new MaterialDto(m.Id, m.Name, m.Category.ToString(), m.Quantity, m.Cost, m.CreatedAt, m.LastAddedQuantity, m.LastAddedTotal))
            .ToListAsync();

        return Ok(materials);
    }

    // 🔹 GET /materials/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var m = await _db.Materials.FindAsync(id);
        if (m == null) return NotFound();
        return Ok(new MaterialDto(m.Id, m.Name, m.Category.ToString(), m.Quantity, m.Cost, m.CreatedAt, m.LastAddedQuantity, m.LastAddedTotal));
    }

    // 🔹 POST /materials
    [HttpPost]
    [Authorize(Roles = "Master, User")]
    public async Task<IActionResult> Create([FromBody] MaterialCreateRequest dto)
    {
        if (!Enum.TryParse<MaterialCategory>(dto.Category, true, out var parsedCategory))
            return BadRequest($"Categoria inválida: {dto.Category}");

        // Verificar se o material já existe (por nome e categoria)
        var existingMaterial = await _db.Materials
            .FirstOrDefaultAsync(m => m.Name.ToLower() == dto.Name.ToLower() && m.Category == parsedCategory);

        if (existingMaterial != null)
        {
            return BadRequest($"The material '{dto.Name}' is already listed under the category'{dto.Category}'. " +
                            $"Please go to the general stock and add the desired quantity to the existing material.");
        }

        var material = new Material
        {
            Name = dto.Name,
            Category = parsedCategory,
            Quantity = dto.Quantity,
            Cost = dto.Cost,
            LastAddedQuantity = dto.Quantity,
            LastAddedTotal = dto.Total
        };

        _db.Materials.Add(material);
        
        // Record initial stock movement
        if (material.Quantity > 0)
        {
            _db.StockMovements.Add(new StockMovement
            {
                MaterialId = material.Id,
                Quantity = material.Quantity,
                MovementType = MovementType.Inbound,
                Note = "Initial stock creation",
                PerformedByUserId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : Guid.Empty,
                ClinicId = null // Warehouse
            });
        }

        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = material.Id },
            new MaterialDto(material.Id, material.Name, material.Category.ToString(), material.Quantity, material.Cost, material.CreatedAt, material.LastAddedQuantity, material.LastAddedTotal));
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
        m.Cost = dto.Cost;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("batch")]
    [Authorize(Roles = "Master, User")]
    public async Task<IActionResult> CreateBatch([FromBody] MaterialCreateBatchRequest request)
    {
        var errors = new List<string>();
        var materialsToAdd = new List<Material>();

        foreach (var item in request.Materials)
        {
            if (!Enum.TryParse<MaterialCategory>(item.Category, true, out var parsedCategory))
            {
                errors.Add($"Categoria inválida para '{item.Name}': {item.Category}");
                continue;
            }

            // Verificar se o material já existe (por nome e categoria)
            var existingMaterial = await _db.Materials
                .FirstOrDefaultAsync(m => m.Name.ToLower() == item.Name.ToLower() && m.Category == parsedCategory);

            if (existingMaterial != null)
            {
errors.Add($"The material '{item.Name}' is already listed under the category '{item.Category}'. " +
           $"Please go to the main inventory and add the required quantity to the existing item.");
                continue;
            }

            var material = new Material
            {
                Name = item.Name,
                Category = parsedCategory,
                Quantity = item.Quantity,
                Cost = item.Cost,
                LastAddedQuantity = item.Quantity,
                LastAddedTotal = item.Total
            };

            materialsToAdd.Add(material);
        }

        if (errors.Any())
        {
            return BadRequest(new { Errors = errors });
        }

        if (materialsToAdd.Any())
        {
            _db.Materials.AddRange(materialsToAdd);
            await _db.SaveChangesAsync();
        }

        return Ok();
    }

    [HttpPost("{id:guid}/add-stock")]
    [Authorize(Roles = "Master, User")]
    public async Task<IActionResult> AddStock(Guid id, [FromBody] MaterialAdjustQuantityRequest request)
    {
        if (request.Quantity <= 0)
            return BadRequest("Quantity must be greater than zero.");

        var material = await _db.Materials.FindAsync(id);
        if (material == null) return NotFound();

        material.Quantity += request.Quantity;
        material.Cost = request.Cost;
        material.CreatedAt = DateTime.UtcNow;
        material.LastAddedQuantity = request.Quantity;
        material.LastAddedTotal = request.Total;

        // Record stock movement for warehouse addition
        _db.StockMovements.Add(new StockMovement
        {
            MaterialId = material.Id,
            Quantity = request.Quantity,
            MovementType = MovementType.Inbound,
            Note = "Added to warehouse stock",
            PerformedByUserId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : Guid.Empty,
            ClinicId = null // Warehouse
        });

        await _db.SaveChangesAsync();

        return Ok(new MaterialDto(material.Id, material.Name, material.Category.ToString(), material.Quantity, material.Cost, material.CreatedAt, material.LastAddedQuantity, material.LastAddedTotal));
    }

    // 🔹 POST /materials/{id}/assign-to-clinic - Transfer material from warehouse to clinic
    [HttpPost("{id:guid}/assign-to-clinic")]
    [Authorize(Roles = "Master")]
    public async Task<IActionResult> AssignToClinic(Guid id, [FromBody] MaterialAssignToClinicRequest request)
    {
        if (request.Quantity <= 0)
            return BadRequest("Quantity must be greater than zero.");

        var material = await _db.Materials.FindAsync(id);
        if (material == null) return NotFound("Material not found.");

        // Check if warehouse has enough stock
        if (material.Quantity < request.Quantity)
            return BadRequest($"Insufficient stock in warehouse. Available: {material.Quantity}, Requested: {request.Quantity}");

        var clinic = await _db.Clinics.FindAsync(request.ClinicId);
        if (clinic == null) return NotFound("Clinic not found.");

        // Get user ID from claims
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized("Unidentified user.");

        // Reduce warehouse stock
        material.Quantity -= request.Quantity;

        // Find or create clinic stock
        var clinicStock = await _db.ClinicStocks
            .FirstOrDefaultAsync(cs => cs.ClinicId == request.ClinicId && cs.MaterialId == id);

        if (clinicStock == null)
        {
            clinicStock = new ClinicStock
            {
                ClinicId = request.ClinicId,
                MaterialId = id,
                QuantityAvailable = request.Quantity
            };
            _db.ClinicStocks.Add(clinicStock);
        }
        else
        {
            clinicStock.QuantityAvailable += request.Quantity;
        }

        // Create stock movement record
        _db.StockMovements.Add(new StockMovement
        {
            ClinicId = request.ClinicId,
            MaterialId = id,
            Quantity = request.Quantity,
            MovementType = MovementType.Transfer,
            Note = $"Transferred from warehouse to {clinic.Name}",
            PerformedByUserId = userId
        });

        await _db.SaveChangesAsync();

        return Ok(new
        {
            Message = $"Successfully assigned {request.Quantity} unit(s) of '{material.Name}' to {clinic.Name}",
            WarehouseQuantity = material.Quantity,
            ClinicQuantity = clinicStock.QuantityAvailable
        });
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
