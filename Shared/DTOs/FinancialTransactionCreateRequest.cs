using Core.Entities.Enums;

namespace Shared.DTOs
{
    public record FinancialTransactionCreateRequest(
        Guid ClinicId,
        TransactionType TransactionType,
        decimal Amount,
        string Description,
        DateTime TransactionDate
    );
}
