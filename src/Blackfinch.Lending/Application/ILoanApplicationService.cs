using Blackfinch.Lending.Domain;

namespace Blackfinch.Lending.Application;

public interface ILoanApplicationService
{
    LoanApplicationResult Submit(LoanApplication application);

    LoanOutcomeSummary GetSummary();
}
