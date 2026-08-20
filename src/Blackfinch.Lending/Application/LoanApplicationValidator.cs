using Blackfinch.Lending.Domain;

namespace Blackfinch.Lending.Application;

public sealed class LoanApplicationValidator : ILoanApplicationValidator
{
    public LoanApplicationValidationResult Validate(LoanApplication application)
    {
        ArgumentNullException.ThrowIfNull(application);

        var errors = new List<string>();

        ValidateMoney(application.LoanAmount, "Loan amount", errors);
        ValidateMoney(application.AssetValue, "Asset value", errors);

        if (application.CreditScore is < 1 or > 999)
        {
            errors.Add("Applicant credit score must be an integer between 1 and 999.");
        }

        return errors.Count == 0
            ? LoanApplicationValidationResult.Success
            : new LoanApplicationValidationResult(errors);
    }

    private static void ValidateMoney(
        decimal value,
        string fieldName,
        ICollection<string> errors)
    {
        if (value <= 0m)
        {
            errors.Add($"{fieldName} must be greater than £0.00.");
            return;
        }

        if (decimal.Round(value, 2) != value)
        {
            errors.Add($"{fieldName} must have no more than two decimal places.");
        }
    }
}
