namespace Shared.DTOs;

public record ClinicConsumeRequest(Guid MaterialId, int Quantity, string? Note = null);

