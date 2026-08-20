namespace Blackfinch.Lending.Domain;

public interface ILoanDecisionService
{
    LoanDecision Decide(LoanApplication application);
}
