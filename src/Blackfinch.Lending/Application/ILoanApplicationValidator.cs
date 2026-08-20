using Blackfinch.Lending.Domain;

namespace Blackfinch.Lending.Application;

public interface ILoanApplicationValidator
{
    LoanApplicationValidationResult Validate(LoanApplication application);
}
