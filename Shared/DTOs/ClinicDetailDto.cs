namespace Shared.DTOs;

public record ClinicDetailDto(Guid Id, string Name, IReadOnlyList<ClinicStockDto> Stocks, IReadOnlyList<StockMovementDto> Movements);
