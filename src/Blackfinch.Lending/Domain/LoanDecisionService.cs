namespace Blackfinch.Lending.Domain;

public sealed class LoanDecisionService : ILoanDecisionService
{
    private const decimal MinimumLoanAmount = 100_000m;
    private const decimal MaximumLoanAmount = 1_500_000m;
    private const decimal HighValueLoanThreshold = 1_000_000m;

    public LoanDecision Decide(LoanApplication application)
    {
        ArgumentNullException.ThrowIfNull(application);

        if (application.AssetValue <= 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(application),
                "Asset value must be greater than zero before a decision can be made.");
        }

        var loanToValue = application.LoanAmount / application.AssetValue * 100m;

        if (application.LoanAmount < MinimumLoanAmount ||
            application.LoanAmount > MaximumLoanAmount)
        {
            return Declined(
                application,
                loanToValue,
                "Loan amount must be between £100,000 and £1,500,000 inclusive.");
        }

        if (application.LoanAmount >= HighValueLoanThreshold)
        {
            if (loanToValue > 60m)
            {
                return Declined(
                    application,
                    loanToValue,
                    "Loans of £1,000,000 or more require an LTV of 60% or less.");
            }

            return application.CreditScore >= 950
                ? Successful(application, loanToValue)
                : Declined(
                    application,
                    loanToValue,
                    "Loans of £1,000,000 or more require a credit score of at least 950.");
        }

        var minimumCreditScore = loanToValue switch
        {
            < 60m => 750,
            < 80m => 800,
            < 90m => 900,
            _ => (int?)null,
        };

        if (minimumCreditScore is null)
        {
            return Declined(
                application,
                loanToValue,
                "Loans below £1,000,000 require an LTV below 90%.");
        }

        return application.CreditScore >= minimumCreditScore.Value
            ? Successful(application, loanToValue)
            : Declined(
                application,
                loanToValue,
                $"An LTV of {loanToValue:F2}% requires a credit score of at least {minimumCreditScore.Value}.");
    }

    private static LoanDecision Successful(
        LoanApplication application,
        decimal loanToValue) =>
        new(
            application,
            LoanDecisionStatus.Successful,
            loanToValue,
            "Application meets the lending criteria.");

    private static LoanDecision Declined(
        LoanApplication application,
        decimal loanToValue,
        string reason) =>
        new(application, LoanDecisionStatus.Declined, loanToValue, reason);
}
