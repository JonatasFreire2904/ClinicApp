namespace Shared.DTOs;

public record ClinicAllocateRequest(Guid MaterialId, int Quantity, string? Note = null);
