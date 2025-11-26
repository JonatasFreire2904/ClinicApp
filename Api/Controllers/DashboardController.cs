using Core.Entities;
using Infrastructure.Dat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.DTOs;

[ApiController]
[Route("dashboard")]
[Authorize(Roles = "Master")]
public class DashboardController(AppDbContext db) : ControllerBase
{
    private readonly AppDbContext _db = db;

    // 🔹 GET /dashboard/financial-summary?startDate=2024-01-01&endDate=2024-12-31&clinicId=guid
    [HttpGet("financial-summary")]
    public async Task<IActionResult> FinancialSummary([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, [FromQuery] Guid? clinicId)
    {
        // Default to current month if not specified
        var start = startDate ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        // Include the entire end date (until 23:59:59) to avoid timezone confusion
        var end = endDate?.Date.AddDays(1).AddTicks(-1) ?? DateTime.UtcNow;

        // Get all Inbound and Transfer stock movements in the date range
        var movementsQuery = _db.StockMovements
            .Include(m => m.Material)
            .Where(m => m.CreatedAt >= start && m.CreatedAt <= end && 
                  (m.MovementType == Core.Entities.Enums.MovementType.Inbound || m.MovementType == Core.Entities.Enums.MovementType.Transfer));

        // Filter by clinic if specified
        if (clinicId.HasValue && clinicId.Value != Guid.Empty)
        {
            movementsQuery = movementsQuery.Where(m => m.ClinicId == clinicId.Value);
        }

        var movements = await movementsQuery.ToListAsync();

        // Calculate total spent: sum of (quantity × material cost) for all Inbound movements ONLY
        // Transfers are internal movements, not new spending
        var inboundMovements = movements.Where(m => m.MovementType == Core.Entities.Enums.MovementType.Inbound).ToList();
        var totalSpent = inboundMovements.Sum(m => m.Quantity * m.Material.Cost);
        var totalMaterialsAdded = inboundMovements.Sum(m => m.Quantity);

        // Get all clinics from database
        var clinics = await _db.Clinics.ToListAsync();

        var clinicExpenses = new List<ClinicExpenseDto>();

        // Group movements by material to show expense breakdown
        // For global breakdown, we show what was purchased (Inbound)
        var materialExpenses = inboundMovements
            .GroupBy(m => new { m.Material.Name, m.Material.Category })
            .Select(g => new MaterialExpenseDto(
                g.Key.Name,
                g.Key.Category.ToString(),
                g.Sum(m => m.Quantity),
                g.Sum(m => m.Quantity * m.Material.Cost),
                g.Max(m => m.CreatedAt)))
            .OrderByDescending(m => m.TotalCost)
            .ToList();

        // Add "All Clinics" aggregated option
        clinicExpenses.Add(new ClinicExpenseDto(
            Guid.Empty,
            "All Clinics",
            totalSpent,
            totalMaterialsAdded,
            materialExpenses));

        // Add each clinic with their specific movements
        foreach (var clinic in clinics)
        {
            // For specific clinic, expense includes Direct Inbound AND Transfers received
            var clinicMovements = movements.Where(m => m.ClinicId == clinic.Id).ToList();
            
            var clinicMaterialExpenses = clinicMovements
                .GroupBy(m => new { m.Material.Name, m.Material.Category })
                .Select(g => new MaterialExpenseDto(
                    g.Key.Name,
                    g.Key.Category.ToString(),
                    g.Sum(m => m.Quantity),
                    g.Sum(m => m.Quantity * m.Material.Cost),
                    g.Max(m => m.CreatedAt)))
                .OrderByDescending(m => m.TotalCost)
                .ToList();

            var clinicTotalCost = clinicMaterialExpenses.Sum(m => m.TotalCost);

            clinicExpenses.Add(new ClinicExpenseDto(
                clinic.Id,
                clinic.Name,
                clinicTotalCost,
                clinicMovements.Sum(m => m.Quantity),
                clinicMaterialExpenses));
        }

        return Ok(new DashboardFinancialSummaryDto(
            start,
            end,
            totalSpent,
            totalMaterialsAdded,
            clinicExpenses));
    }

    [HttpGet("clinic/{id:guid}")]
    public async Task<IActionResult> ClinicDetails(Guid id)
    {
        var clinic = await _db.Clinics
            .Include(c => c.ClinicStocks)
                .ThenInclude(s => s.Material)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (clinic == null) return NotFound();

        var stocks = clinic.ClinicStocks
            .Where(s => s.QuantityAvailable > 0)
            .Select(s => new ClinicStockDto(s.MaterialId, s.Material.Name, s.QuantityAvailable, s.Material.Category.ToString(), s.IsOpen, s.OpenedAt))
            .ToList();

        var movements = await _db.StockMovements
            .Include(m => m.Material)
            .Include(m => m.PerformedByUser)
            .Where(m => m.ClinicId == id)
            .OrderByDescending(m => m.CreatedAt)
            .Take(20)
            .Select(m => new StockMovementDto(m.Id, m.ClinicId ?? Guid.Empty, m.MaterialId, m.Material.Name, m.Quantity, m.MovementType.ToString(), m.PerformedByUser.UserName, m.CreatedAt, m.Note))
            .ToListAsync();

        return Ok(new
        {
            clinic.Id,
            clinic.Name,
            Stocks = stocks,
            RecentMovements = movements
        });
    }
}
