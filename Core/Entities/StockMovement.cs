using Core.Entities.Enums;

namespace Core.Entities
{
    public class StockMovement
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid? ClinicId { get; set; }
        public Clinic? Clinic { get; set; }

        public Guid MaterialId { get; set; }
        public Material Material { get; set; }

        public Guid PerformedByUserId { get; set; }
        public User PerformedByUser { get; set; }

        public MovementType MovementType { get; set; }
        public int Quantity { get; set; }
        public string Note { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
