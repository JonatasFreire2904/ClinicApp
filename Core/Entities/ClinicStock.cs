using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities
{
    public class ClinicStock
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ClinicId { get; set; }
        public Clinic Clinic { get; set; }

        public Guid MaterialId { get; set; }
        public Material Material { get; set; }

        public int QuantityAvailable { get; set; }
        public bool IsOpen { get; set; }
        public DateTime? OpenedAt { get; set; }
    }
}
