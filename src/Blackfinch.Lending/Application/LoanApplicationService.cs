using Blackfinch.Lending.Domain;

namespace Blackfinch.Lending.Application;

public sealed class LoanApplicationService : ILoanApplicationService
{
    private readonly ILoanApplicationValidator _validator;
    private readonly ILoanDecisionService _decisionService;
    private readonly ILoanOutcomeStore _outcomeStore;

    public LoanApplicationService(
        ILoanApplicationValidator validator,
        ILoanDecisionService decisionService,
        ILoanOutcomeStore outcomeStore)
    {
        _validator = validator;
        _decisionService = decisionService;
        _outcomeStore = outcomeStore;
    }

    public LoanApplicationResult Submit(LoanApplication application)
    {
        var validationResult = _validator.Validate(application);
        if (!validationResult.IsValid)
        {
            return LoanApplicationResult.Invalid(
                validationResult.Errors,
                _outcomeStore.GetSummary());
        }

        var decision = _decisionService.Decide(application);
        var summary = _outcomeStore.Record(decision);

        return LoanApplicationResult.Valid(decision, summary);
    }

    public LoanOutcomeSummary GetSummary() => _outcomeStore.GetSummary();
}
