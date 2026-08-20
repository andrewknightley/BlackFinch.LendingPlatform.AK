using Blackfinch.Lending.Domain;

namespace Blackfinch.Lending.Application;

public interface ILoanOutcomeStore
{
    LoanOutcomeSummary Record(LoanDecision decision);

    LoanOutcomeSummary GetSummary();
}
