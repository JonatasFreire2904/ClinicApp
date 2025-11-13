namespace Shared.DTOs;

public record ClinicAddStockRequest(Guid MaterialId, int Quantity, string? Note = null);

