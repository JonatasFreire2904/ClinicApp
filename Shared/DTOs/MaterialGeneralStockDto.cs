namespace Shared.DTOs;

public record MaterialGeneralStockDto(
    Guid Id,
    string Name,
    string Category,
    int WarehouseQuantity,
    int TotalQuantity,
    decimal Cost,
    DateTime CreatedAt,
    int LastAddedQuantity,
    decimal LastAddedTotal,
    IReadOnlyList<MaterialClinicStockDto> Clinics);

public record MaterialClinicStockDto(
    Guid ClinicId,
    string ClinicName,
    int Quantity);

