using Blackfinch.Lending.Domain;

namespace Blackfinch.Lending.Application;

public sealed record LoanApplicationResult(
    LoanDecision? Decision,
    LoanOutcomeSummary Summary,
    IReadOnlyList<string> ValidationErrors)
{
    public bool IsValid => ValidationErrors.Count == 0;

    public static LoanApplicationResult Invalid(
        IReadOnlyList<string> errors,
        LoanOutcomeSummary summary) =>
        new(null, summary, errors);

    public static LoanApplicationResult Valid(
        LoanDecision decision,
        LoanOutcomeSummary summary) =>
        new(decision, summary, []);
}
