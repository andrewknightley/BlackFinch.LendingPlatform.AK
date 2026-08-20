namespace Blackfinch.Lending.Application;

public sealed record LoanApplicationValidationResult(IReadOnlyList<string> Errors)
{
    public bool IsValid => Errors.Count == 0;

    public static LoanApplicationValidationResult Success { get; } = new([]);
}
