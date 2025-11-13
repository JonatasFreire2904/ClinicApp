namespace Shared.DTOs;

public record MaterialGeneralStockDto(
    Guid Id,
    string Name,
    string Category,
    int TotalQuantity,
    IReadOnlyList<MaterialClinicStockDto> Clinics);

public record MaterialClinicStockDto(
    Guid ClinicId,
    string ClinicName,
    int Quantity);

