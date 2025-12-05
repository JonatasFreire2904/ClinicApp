using Core.Entities.Enums;

namespace Shared.DTOs
{
    public record FinancialTransactionDto(
        Guid Id,
        Guid ClinicId,
        string ClinicName,
        TransactionType TransactionType,
        decimal Amount,
        string Description,
        DateTime TransactionDate,
        Guid CreatedByUserId,
        string CreatedByUserName,
        DateTime CreatedAt
    );
}
