namespace Shared.DTOs;

public record MaterialCreateRequest(string Name, string Category, int Quantity, decimal Cost, decimal Total);
