namespace Shared.DTOs
{
    public record MaterialDto(Guid Id, string Name, string Category, int Quantity, decimal Cost, DateTime CreatedAt, int LastAddedQuantity, decimal LastAddedTotal);
}
