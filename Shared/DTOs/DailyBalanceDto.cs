namespace Shared.DTOs
{
    public record DailyBalanceDto(
        DateTime Date,
        decimal TotalIncome,
        decimal TotalExpenses,
        decimal Balance,
        List<FinancialTransactionDto> Transactions
    );
}
