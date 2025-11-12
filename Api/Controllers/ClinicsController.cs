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
public class ClinicsController : ControllerBase
{
    private readonly AppDbContext _db;
    public ClinicsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var clinics = await _db.Clinics
            .Select(c => new ClinicDto(c.Id, c.Name))
            .ToListAsync();
        return Ok(clinics);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var clinic = await _db.Clinics
            .Include(c => c.ClinicStocks)
                .ThenInclude(s => s.Material)
            .Include(c => c.UserClinics)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (clinic == null) return NotFound();

        var stocks = clinic.ClinicStocks
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
            .Where(m => m.ClinicId == id)
            .OrderByDescending(m => m.CreatedAt)
            .Take(50)
            .Select(m => new StockMovementDto(
                m.Id,
                m.ClinicId,
                m.MaterialId,
                m.Quantity,
                m.MovementType.ToString(),
                m.PerformedByUser.UserName,
                m.CreatedAt,
                m.Note))
            .ToListAsync();

        return Ok(new ClinicDetailDto(clinic.Id, clinic.Name, stocks, movements));
    }

    [HttpPost("{clinicId:guid}/allocate")]
    [Authorize(Roles = "Master")]
    public async Task<IActionResult> AllocateToClinic(Guid clinicId, [FromBody] ClinicAllocateRequest request)
    {
        if (request.Quantity <= 0)
            return BadRequest("Quantidade deve ser maior que zero.");

        var clinic = await _db.Clinics
            .Include(c => c.ClinicStocks)
            .FirstOrDefaultAsync(c => c.Id == clinicId);
        if (clinic == null) return NotFound("Clínica não encontrada.");

        var material = await _db.Materials.FindAsync(request.MaterialId);
        if (material == null) return NotFound("Material não encontrado.");

        if (material.Quantity < request.Quantity)
            return BadRequest("Quantidade insuficiente no estoque geral.");

        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized("Usuário não identificado.");

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
            MovementType = MovementType.Entrada,
            Note = string.IsNullOrWhiteSpace(request.Note)
                ? $"Distribuição do estoque geral"
                : request.Note!,
            PerformedByUserId = userId
        });

        await _db.SaveChangesAsync();

        return Ok(new
        {
            Material = new MaterialDto(material.Id, material.Name, material.Category.ToString(), material.Quantity),
            ClinicStock = new ClinicStockDto(clinicStock.MaterialId, material.Name, clinicStock.QuantityAvailable, material.Category.ToString(), clinicStock.IsOpen, clinicStock.OpenedAt)
        });
    }

    [HttpPost("{clinicId:guid}/stock/{materialId:guid}/open")]
    [Authorize(Roles = "Master")]
    public async Task<IActionResult> SetClinicStockOpen(Guid clinicId, Guid materialId, [FromBody] ClinicStockOpenRequest request)
    {
        var clinicStock = await _db.ClinicStocks
            .Include(cs => cs.Material)
            .FirstOrDefaultAsync(cs => cs.ClinicId == clinicId && cs.MaterialId == materialId);

        if (clinicStock == null)
            return NotFound("Registro de estoque não encontrado.");

        if (clinicStock.Material.Category != MaterialCategory.MateriaisDeUso)
            return BadRequest("Apenas materiais de uso podem ser marcados como abertos.");

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
        // Usuários podem ver todas as clínicas e transitar entre elas
        var clinics = await _db.Clinics
            .Select(c => new ClinicDto(c.Id, c.Name))
            .ToListAsync();

        return Ok(clinics);
    }

    [HttpPost("{clinicId:guid}/consume")]
    [Authorize(Roles = "User")]
    public async Task<IActionResult> ConsumeMaterial(Guid clinicId, [FromBody] ClinicConsumeRequest request)
    {
        if (request.Quantity <= 0)
            return BadRequest("Quantidade deve ser maior que zero.");

        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized("Usuário não identificado.");

        // Usuários podem consumir de qualquer clínica (não precisa estar associado)
        var clinic = await _db.Clinics
            .Include(c => c.ClinicStocks)
            .FirstOrDefaultAsync(c => c.Id == clinicId);

        if (clinic == null) return NotFound("Clínica não encontrada.");

        var clinicStock = await _db.ClinicStocks
            .FirstOrDefaultAsync(cs => cs.ClinicId == clinicId && cs.MaterialId == request.MaterialId);

        if (clinicStock == null)
            return NotFound("Material não encontrado no estoque desta clínica.");

        if (clinicStock.QuantityAvailable < request.Quantity)
            return BadRequest("Quantidade insuficiente no estoque da clínica.");

        // Diminui a quantidade no estoque da clínica
        clinicStock.QuantityAvailable -= request.Quantity;

        // Registra o movimento de saída
        _db.StockMovements.Add(new StockMovement
        {
            ClinicId = clinicId,
            MaterialId = request.MaterialId,
            Quantity = request.Quantity,
            MovementType = MovementType.Saida,
            Note = string.IsNullOrWhiteSpace(request.Note)
                ? $"Consumo de material"
                : request.Note!,
            PerformedByUserId = userId
        });

        await _db.SaveChangesAsync();

        return Ok(new
        {
            Message = "Material consumido com sucesso.",
            RemainingQuantity = clinicStock.QuantityAvailable
        });
    }
}
