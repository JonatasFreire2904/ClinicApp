namespace Shared.DTOs
{
    public record FinancialDashboardSummaryDto(
        DateTime StartDate,
        DateTime EndDate,
        decimal TotalIncome,
        decimal TotalExpense,
        decimal NetBalance,
        List<DailyFinancialStatsDto> DailyHistory,
        List<ClinicFinancialPerformanceDto> ClinicPerformance
    );

    public record DailyFinancialStatsDto(
        DateTime Date,
        decimal Income,
        decimal Expense,
        decimal Balance
    );

    public record ClinicFinancialPerformanceDto(
        Guid ClinicId,
        string ClinicName,
        decimal Income,
        decimal Expense,
        decimal Balance
    );
}
