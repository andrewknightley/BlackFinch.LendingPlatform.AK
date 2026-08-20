namespace Blackfinch.Lending.Application;

public sealed record LoanOutcomeSummary(
    int SuccessfulApplicantCount,
    int DeclinedApplicantCount,
    decimal TotalValueOfLoansWritten,
    decimal MeanLoanToValuePercentage)
{
    public static LoanOutcomeSummary Empty { get; } = new(0, 0, 0m, 0m);
}
