using Infrastructure.Dat;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.DTOs;

[ApiController]
[Route("dashboard")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;
    public DashboardController(AppDbContext db) => _db = db;

    [HttpGet("summary")]
    public async Task<IActionResult> Summary()
    {
        // por clinica: qtd materiais distintos e soma das quantidades
        var q = await _db.Clinics
            .Select(c => new ClinicSummaryDto
            {
                ClinicId = c.Id,
                ClinicName = c.Name,
                DistinctMaterials = c.ClinicStocks.Count(),
                TotalQuantity = c.ClinicStocks.Sum(s => s.QuantityAvailable)
            }).ToListAsync();

        var dto = new DashboardSummaryDto { Clinics = q };
        return Ok(dto);
    }

    [HttpGet("clinic/{id:guid}")]
    public async Task<IActionResult> ClinicDetails(Guid id)
    {
        var clinic = await _db.Clinics
            .Where(c => c.Id == id)
            .Select(c => new {
                c.Id,
                c.Name,
                Stocks = c.ClinicStocks.Select(s => new ClinicStockDto(s.MaterialId, s.Material.Name, s.QuantityAvailable, s.Material.Category.ToString(), s.IsOpen, s.OpenedAt)),
                RecentMovements = _db.StockMovements
                    .Where(m => m.ClinicId == id)
                    .OrderByDescending(m => m.CreatedAt)
                    .Take(20)
                    .Select(m => new StockMovementDto(m.Id, m.ClinicId, m.MaterialId, m.Quantity, m.MovementType.ToString(), m.PerformedByUser.UserName, m.CreatedAt, m.Note))
            }).FirstOrDefaultAsync();

        if (clinic == null) return NotFound();
        return Ok(clinic);
    }
}
