namespace Blackfinch.Lending.Domain;

public sealed record LoanDecision(
    LoanApplication Application,
    LoanDecisionStatus Status,
    decimal LoanToValuePercentage,
    string Reason);
