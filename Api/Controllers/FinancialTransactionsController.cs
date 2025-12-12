using Core.Entities;
using Core.Entities.Enums;
using Infrastructure.Dat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.DTOs;
using System.Security.Claims;

namespace Api.Controllers
{
    [ApiController]
    [Route("financial-transactions")]
    [Authorize]
    public class FinancialTransactionsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FinancialTransactionsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/financial-transactions?clinicId={id}&date={date}
        [HttpGet]
        public async Task<ActionResult<List<FinancialTransactionDto>>> GetTransactions(
            [FromQuery] Guid? clinicId,
            [FromQuery] DateTime? date)
        {
            // System design: All users can view transactions from any clinic
            var query = _context.FinancialTransactions
                .Include(t => t.Clinic)
                .Include(t => t.CreatedByUser)
                .AsQueryable();

            // Filter by clinic
            if (clinicId.HasValue)
            {
                query = query.Where(t => t.ClinicId == clinicId.Value);
            }

            // Filter by date
            if (date.HasValue)
            {
                var dateOnly = date.Value.Date;
                query = query.Where(t => t.TransactionDate.Date == dateOnly);
            }

            var transactions = await query
                .OrderByDescending(t => t.TransactionDate)
                .ThenByDescending(t => t.CreatedAt)
                .Select(t => new FinancialTransactionDto(
                    t.Id,
                    t.ClinicId,
                    t.Clinic.Name,
                    t.TransactionType,
                    t.Amount,
                    t.Description,
                    t.TransactionDate,
                    t.CreatedByUserId,
                    t.CreatedByUser.UserName,
                    t.CreatedAt
                ))
                .ToListAsync();

            return Ok(transactions);
        }

        // GET: api/financial-transactions/daily-balance?clinicId={id}&date={date}
        [HttpGet("daily-balance")]
        public async Task<ActionResult<DailyBalanceDto>> GetDailyBalance(
            [FromQuery] Guid clinicId,
            [FromQuery] DateTime date)
        {
            // System design: Users can access any clinic without UserClinics association

            var dateOnly = date.Date;
            var transactions = await _context.FinancialTransactions
                .Include(t => t.Clinic)
                .Include(t => t.CreatedByUser)
                .Where(t => t.ClinicId == clinicId && t.TransactionDate.Date == dateOnly)
                .OrderBy(t => t.CreatedAt)
                .Select(t => new FinancialTransactionDto(
                    t.Id,
                    t.ClinicId,
                    t.Clinic.Name,
                    t.TransactionType,
                    t.Amount,
                    t.Description,
                    t.TransactionDate,
                    t.CreatedByUserId,
                    t.CreatedByUser.UserName,
                    t.CreatedAt
                ))
                .ToListAsync();

            var totalIncome = transactions
                .Where(t => t.TransactionType == TransactionType.Income)
                .Sum(t => t.Amount);

            var totalExpenses = transactions
                .Where(t => t.TransactionType == TransactionType.Expense)
                .Sum(t => t.Amount);

            var balance = totalIncome - totalExpenses;

            return Ok(new DailyBalanceDto(
                dateOnly,
                totalIncome,
                totalExpenses,
                balance,
                transactions.ToList()
            ));
        }

        // GET: financial-transactions/dashboard
        [HttpGet("dashboard")]
        [Authorize(Roles = "Master")]
        public async Task<ActionResult<FinancialDashboardSummaryDto>> GetDashboardSummary(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] Guid? clinicId)
        {
            var start = startDate?.Date ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var end = endDate?.Date.AddDays(1).AddTicks(-1) ?? DateTime.UtcNow.Date.AddDays(1).AddTicks(-1);

            var query = _context.FinancialTransactions
                .Include(t => t.Clinic)
                .Where(t => t.TransactionDate >= start && t.TransactionDate <= end);

            if (clinicId.HasValue && clinicId.Value != Guid.Empty)
            {
                query = query.Where(t => t.ClinicId == clinicId.Value);
            }

            var transactions = await query.ToListAsync();

            var totalIncome = transactions.Where(t => t.TransactionType == TransactionType.Income).Sum(t => t.Amount);
            var totalExpense = transactions.Where(t => t.TransactionType == TransactionType.Expense).Sum(t => t.Amount);
            var netBalance = totalIncome - totalExpense;

            // Daily History
            var dailyHistory = transactions
                .GroupBy(t => t.TransactionDate.Date)
                .Select(g => new DailyFinancialStatsDto(
                    g.Key,
                    g.Where(t => t.TransactionType == TransactionType.Income).Sum(t => t.Amount),
                    g.Where(t => t.TransactionType == TransactionType.Expense).Sum(t => t.Amount),
                    g.Where(t => t.TransactionType == TransactionType.Income).Sum(t => t.Amount) - 
                    g.Where(t => t.TransactionType == TransactionType.Expense).Sum(t => t.Amount)
                ))
                .OrderBy(d => d.Date)
                .ToList();

            // Clinic Performance
            var clinicPerformance = transactions
                .GroupBy(t => new { t.ClinicId, t.Clinic.Name })
                .Select(g => new ClinicFinancialPerformanceDto(
                    g.Key.ClinicId,
                    g.Key.Name,
                    g.Where(t => t.TransactionType == TransactionType.Income).Sum(t => t.Amount),
                    g.Where(t => t.TransactionType == TransactionType.Expense).Sum(t => t.Amount),
                    g.Where(t => t.TransactionType == TransactionType.Income).Sum(t => t.Amount) - 
                    g.Where(t => t.TransactionType == TransactionType.Expense).Sum(t => t.Amount)
                ))
                .OrderByDescending(c => c.Balance)
                .ToList();

            return Ok(new FinancialDashboardSummaryDto(
                start,
                end,
                totalIncome,
                totalExpense,
                netBalance,
                dailyHistory,
                clinicPerformance
            ));
        }

        // POST: api/financial-transactions
        [HttpPost]
        public async Task<ActionResult<FinancialTransactionDto>> CreateTransaction(
            [FromBody] FinancialTransactionCreateRequest request)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // System design: Users can create transactions for any clinic

            // Validate clinic exists
            var clinic = await _context.Clinics.FindAsync(request.ClinicId);
            if (clinic == null)
            {
                return BadRequest("Clinic not found");
            }

            var transaction = new FinancialTransaction
            {
                ClinicId = request.ClinicId,
                TransactionType = request.TransactionType,
                Amount = request.Amount,
                Description = request.Description,
                TransactionDate = request.TransactionDate.Date,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.FinancialTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            // Load navigation properties
            await _context.Entry(transaction).Reference(t => t.Clinic).LoadAsync();
            await _context.Entry(transaction).Reference(t => t.CreatedByUser).LoadAsync();

            var dto = new FinancialTransactionDto(
                transaction.Id,
                transaction.ClinicId,
                transaction.Clinic.Name,
                transaction.TransactionType,
                transaction.Amount,
                transaction.Description,
                transaction.TransactionDate,
                transaction.CreatedByUserId,
                transaction.CreatedByUser.UserName,
                transaction.CreatedAt
            );

            return CreatedAtAction(nameof(GetTransactions), new { }, dto);
        }

        // DELETE: api/financial-transactions/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTransaction(Guid id)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            var transaction = await _context.FinancialTransactions.FindAsync(id);
            if (transaction == null)
            {
                return NotFound();
            }

            // Authorization: Only the creator or Master can delete
            if (userRole != "Master" && transaction.CreatedByUserId != userId)
            {
                return Forbid();
            }

            _context.FinancialTransactions.Remove(transaction);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
