using Core.Entities.Enums;

namespace Core.Entities
{
    public class Material
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public MaterialCategory Category { get; set; }
        public int Quantity { get; set; }
        public decimal Cost { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int LastAddedQuantity { get; set; }
        public decimal LastAddedTotal { get; set; }
    }
}
