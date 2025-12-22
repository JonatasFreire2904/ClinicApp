using Core.Entities;
using Core.Entities.Enums   ;
using Infrastructure.Dat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.DTOs;
using System.Security.Claims;

[ApiController]
[Route("clinics")]
public class ClinicsController(AppDbContext db) : ControllerBase
{
    private readonly AppDbContext _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var clinics = await _db.Clinics
            .Select(c => new ClinicDto(c.Id, c.Name))
            .ToListAsync();
        return Ok(clinics);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "User, Master")]
    public async Task<IActionResult> Get(Guid id)
    {
        var clinic = await _db.Clinics
            .Include(c => c.ClinicStocks)
                .ThenInclude(s => s.Material)
            .Include(c => c.UserClinics)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (clinic == null) return NotFound();

        var stocks = clinic.ClinicStocks
            //.Where(s => s.QuantityAvailable > 0) // Allow zero quantity items to be listed
            .OrderBy(s => s.Material.Name)
            .Select(s => new ClinicStockDto(
                s.MaterialId,
                s.Material.Name,
                s.QuantityAvailable,
                s.Material.Category.ToString(),
                s.IsOpen,
                s.OpenedAt))
            .ToList();

        var movements = await _db.StockMovements
            .Include(m => m.Material)
            .Where(m => m.ClinicId == id)
            .OrderByDescending(m => m.CreatedAt)
            .Take(50)
            .Select(m => new StockMovementDto(
                m.Id,
                m.ClinicId ?? Guid.Empty,
                m.MaterialId,
                m.Material.Name,
                m.Quantity,
                m.MovementType.ToString(),
                m.PerformedByUser.UserName,
                m.CreatedAt,
                m.Note))
            .ToListAsync();

        return Ok(new ClinicDetailDto(clinic.Id, clinic.Name, stocks, movements));
    }

    [HttpPost("{clinicId:guid}/allocate")]
    [Authorize(Roles = "User, Master")]
    public async Task<IActionResult> AllocateToClinic(Guid clinicId, [FromBody] ClinicAllocateRequest request)
    {
        if (request.Quantity <= 0)
            return BadRequest("Quantity must be greater than zero.");

        var clinic = await _db.Clinics
            .Include(c => c.ClinicStocks)
            .FirstOrDefaultAsync(c => c.Id == clinicId);
        if (clinic == null) return NotFound("No clinic found.");

        var material = await _db.Materials.FindAsync(request.MaterialId);
        if (material == null) return NotFound("The requested material could not be found.");

        if (material.Quantity < request.Quantity)
            return BadRequest("Insufficient quantity in general inventory.");

        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized("Unidentified user.");

        material.Quantity -= request.Quantity;

        var clinicStock = await _db.ClinicStocks
            .FirstOrDefaultAsync(cs => cs.ClinicId == clinicId && cs.MaterialId == material.Id);

        if (clinicStock is null)
        {
            clinicStock = new ClinicStock
            {
                ClinicId = clinicId,
                MaterialId = material.Id,
                QuantityAvailable = request.Quantity,
                IsOpen = false,
                OpenedAt = null
            };
            _db.ClinicStocks.Add(clinicStock);
        }
        else
        {
            clinicStock.QuantityAvailable += request.Quantity;
        }

        _db.StockMovements.Add(new StockMovement
        {
            ClinicId = clinicId,
            MaterialId = material.Id,
            Quantity = request.Quantity,

            MovementType = MovementType.Transfer,
            Note = string.IsNullOrWhiteSpace(request.Note)
                ? $"General inventory distribution."
                : request.Note!,
            PerformedByUserId = userId
        });

        await _db.SaveChangesAsync();

        return Ok(new
        {
            Material = new MaterialDto(material.Id, material.Name, material.Category.ToString(), material.Quantity, material.Cost, material.CreatedAt, material.LastAddedQuantity, material.LastAddedTotal),
            ClinicStock = new ClinicStockDto(clinicStock.MaterialId, material.Name, clinicStock.QuantityAvailable, material.Category.ToString(), clinicStock.IsOpen, clinicStock.OpenedAt)
        });
    }

    [HttpPost("{clinicId:guid}/stock/add")]
    [Authorize(Roles = "User, Master")]
    public async Task<IActionResult> AddStockToClinic(Guid clinicId, [FromBody] ClinicAddStockRequest request)
    {
        if (request.Quantity <= 0)
            return BadRequest("Quantity must be greater than zero.");

        var clinic = await _db.Clinics
            .Include(c => c.ClinicStocks)
            .FirstOrDefaultAsync(c => c.Id == clinicId);
        if (clinic == null) return NotFound("The specified clinic could not be found.");

        var material = await _db.Materials.FindAsync(request.MaterialId);
        if (material == null) return NotFound("Material not found.");

        // Check if there's enough quantity in the warehouse
        if (material.Quantity < request.Quantity)
            return BadRequest("Insufficient quantity in warehouse.");

        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized("Unidentified user.");

        // Subtract from warehouse
        material.Quantity -= request.Quantity;

        var clinicStock = await _db.ClinicStocks
            .FirstOrDefaultAsync(cs => cs.ClinicId == clinicId && cs.MaterialId == material.Id);

        if (clinicStock is null)
        {
            clinicStock = new ClinicStock
            {
                ClinicId = clinicId,
                MaterialId = material.Id,
                QuantityAvailable = request.Quantity,
                IsOpen = false,
                OpenedAt = null
            };
            _db.ClinicStocks.Add(clinicStock);
        }
        else
        {
            clinicStock.QuantityAvailable += request.Quantity;
        }

        _db.StockMovements.Add(new StockMovement
        {
            ClinicId = clinicId,
            MaterialId = material.Id,
            Quantity = request.Quantity,
            MovementType = MovementType.Inbound,
            Note = string.IsNullOrWhiteSpace(request.Note)
                ? "Direct entry into the clinic's inventory."
                : request.Note!,
            PerformedByUserId = userId
        });

        await _db.SaveChangesAsync();

        return Ok(new ClinicStockDto(
            clinicStock.MaterialId,
            material.Name,
            clinicStock.QuantityAvailable,
            material.Category.ToString(),
            clinicStock.IsOpen,
            clinicStock.OpenedAt));
    }

    [HttpPost("{clinicId:guid}/stock/{materialId:guid}/open")]
    [Authorize(Roles = "User, Master")]
    public async Task<IActionResult> SetClinicStockOpen(Guid clinicId, Guid materialId, [FromBody] ClinicStockOpenRequest request)
    {
        var clinicStock = await _db.ClinicStocks
            .Include(cs => cs.Material)
            .FirstOrDefaultAsync(cs => cs.ClinicId == clinicId && cs.MaterialId == materialId);

        if (clinicStock == null)
            return NotFound("Registro de estoque não encontrado.");

        // REMOVED: Category restriction. Now all materials can be opened/closed.
        // if (clinicStock.Material.Category != MaterialCategory.UsageMaterials && 
        //     clinicStock.Material.Category != MaterialCategory.Disposables)
        //     return BadRequest("Only consumable and disposable materials can be marked as opened.");

        if (request.IsOpen && !clinicStock.IsOpen)
        {
            clinicStock.IsOpen = true;
            clinicStock.OpenedAt = DateTime.UtcNow;
        }
        else if (!request.IsOpen && clinicStock.IsOpen)
        {
            clinicStock.IsOpen = false;
            clinicStock.OpenedAt = null;
        }

        await _db.SaveChangesAsync();

        return Ok(new ClinicStockDto(
            clinicStock.MaterialId,
            clinicStock.Material.Name,
            clinicStock.QuantityAvailable,
            clinicStock.Material.Category.ToString(),
            clinicStock.IsOpen,
            clinicStock.OpenedAt));
    }

    [HttpPost]
    [Authorize(Roles = "Master")]
    public async Task<IActionResult> Create([FromBody] ClinicDto dto)
    {
        var clinic = new Clinic { Name = dto.Name };
        _db.Clinics.Add(clinic);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = clinic.Id }, new ClinicDto(clinic.Id, clinic.Name));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Master")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ClinicDto dto)
    {
        var clinic = await _db.Clinics.FindAsync(id);
        if (clinic == null) return NotFound();
        clinic.Name = dto.Name;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Master")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var clinic = await _db.Clinics.FindAsync(id);
        if (clinic == null) return NotFound();
        _db.Clinics.Remove(clinic);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("my-clinics")]
    [Authorize(Roles = "User, Master")]
    public async Task<IActionResult> GetMyClinics()
    {
        // Users can see all clinics and navigate between them
        var clinics = await _db.Clinics
            .Select(c => new ClinicSummaryDto
            {
                ClinicId = c.Id,
                ClinicName = c.Name,
                DistinctMaterials = 0,
                TotalQuantity = 0
            })
            .ToListAsync();

        return Ok(clinics);
    }

    [HttpPost("{clinicId:guid}/consume")]
    [Authorize(Roles = "User")]
    public async Task<IActionResult> ConsumeMaterial(Guid clinicId, [FromBody] ClinicConsumeRequest request)
    {
        if (request.Quantity <= 0)
            return BadRequest("Quantity must be greater than zero.");

        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized("Unidentified user.");

        // Usuários podem consumir de qualquer clínica (não precisa estar associado)
        var clinic = await _db.Clinics
            .Include(c => c.ClinicStocks)
            .FirstOrDefaultAsync(c => c.Id == clinicId);

        if (clinic == null) return NotFound("The specified clinic could not be found.");

        var clinicStock = await _db.ClinicStocks
            .FirstOrDefaultAsync(cs => cs.ClinicId == clinicId && cs.MaterialId == request.MaterialId);

        if (clinicStock == null)
            return NotFound("Material not found in this clinic's inventory.");

        if (clinicStock.QuantityAvailable < request.Quantity)
            return BadRequest("Insufficient quantity in the clinic's inventory.");

        // Diminui a quantidade no estoque da clínica
        clinicStock.QuantityAvailable -= request.Quantity;

        // Registra o movimento de saída
        _db.StockMovements.Add(new StockMovement
        {
            ClinicId = clinicId,
            MaterialId = request.MaterialId,
            Quantity = request.Quantity,
            MovementType = MovementType.Outbound,
            Note = string.IsNullOrWhiteSpace(request.Note)
                ? $"Material consumption"
                : request.Note!,
            PerformedByUserId = userId
        });

        await _db.SaveChangesAsync();

        return Ok(new
        {
            Message = "Material successfully consumed.",
            RemainingQuantity = clinicStock.QuantityAvailable
        });
    }

    [HttpDelete("{clinicId:guid}/movements")]
    [Authorize(Roles = "User, Master")]
    public async Task<IActionResult> ClearMovements(Guid clinicId)
    {
        var clinic = await _db.Clinics.FindAsync(clinicId);
        if (clinic == null) return NotFound("The specified clinic could not be found.");

        var movements = await _db.StockMovements
            .Where(m => m.ClinicId == clinicId)
            .ToListAsync();

        _db.StockMovements.RemoveRange(movements);
        await _db.SaveChangesAsync();

        return Ok(new { Message = "Movement log successfully cleared." });
    }
}
