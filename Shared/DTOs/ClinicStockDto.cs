using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs
{
    public record ClinicStockDto(Guid MaterialId, string MaterialName, int QuantityAvailable, string Category, bool IsOpen, DateTime? OpenedAt);

}
