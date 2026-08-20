using Blackfinch.Lending.Domain;

namespace Blackfinch.Lending.Application;

public sealed class InMemoryLoanOutcomeStore : ILoanOutcomeStore
{
    private readonly Lock _syncRoot = new();
    private int _successfulApplicantCount;
    private int _declinedApplicantCount;
    private decimal _totalValueOfLoansWritten;
    private decimal _totalLoanToValuePercentage;

    public LoanOutcomeSummary Record(LoanDecision decision)
    {
        ArgumentNullException.ThrowIfNull(decision);

        lock (_syncRoot)
        {
            if (decision.Status is LoanDecisionStatus.Successful)
            {
                _successfulApplicantCount++;
                _totalValueOfLoansWritten += decision.Application.LoanAmount;
            }
            else
            {
                _declinedApplicantCount++;
            }

            _totalLoanToValuePercentage += decision.LoanToValuePercentage;

            return CreateSummary();
        }
    }

    public LoanOutcomeSummary GetSummary()
    {
        lock (_syncRoot)
        {
            return CreateSummary();
        }
    }

    private LoanOutcomeSummary CreateSummary()
    {
        var totalApplicantCount = _successfulApplicantCount + _declinedApplicantCount;
        var meanLoanToValue = totalApplicantCount == 0
            ? 0m
            : _totalLoanToValuePercentage / totalApplicantCount;

        return new LoanOutcomeSummary(
            _successfulApplicantCount,
            _declinedApplicantCount,
            _totalValueOfLoansWritten,
            meanLoanToValue);
    }
}
