namespace Shared.DTOs;

public record DashboardFinancialSummaryDto(
    DateTime StartDate,
    DateTime EndDate,
    decimal TotalSpent,
    int TotalMaterialsAdded,
    IReadOnlyList<ClinicExpenseDto> ClinicExpenses);

public record ClinicExpenseDto(
    Guid ClinicId,
    string ClinicName,
    decimal TotalSpent,
    int MaterialsAdded,
    IReadOnlyList<MaterialExpenseDto> Materials);

public record MaterialExpenseDto(
    string MaterialName,
    string Category,
    int QuantityAdded,
    decimal TotalCost,
    DateTime LastAdded);
