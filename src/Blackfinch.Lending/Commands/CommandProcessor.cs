using System.Globalization;
using Blackfinch.Lending.Application;
using Blackfinch.Lending.Domain;

namespace Blackfinch.Lending.Commands;

public sealed class CommandProcessor : ICommandProcessor
{
    private const string Usage =
        "Usage: apply <loan-amount> <asset-value> <credit-score>";

    private static readonly CultureInfo GbCulture = CultureInfo.GetCultureInfo("en-GB");
    private static readonly NumberStyles MoneyStyles =
        NumberStyles.Number | NumberStyles.AllowCurrencySymbol;

    private readonly ILoanApplicationService _loanApplicationService;

    public CommandProcessor(ILoanApplicationService loanApplicationService)
    {
        _loanApplicationService = loanApplicationService;
    }

    public CommandResult Process(string? input)
    {
        var arguments = input?.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];

        if (arguments.Length == 0)
        {
            return CommandResult.From($"No command supplied. {Usage}");
        }

        return arguments[0].ToLowerInvariant() switch
        {
            "apply" => ProcessApplication(arguments),
            "stats" => CommandResult.From(FormatSummary(_loanApplicationService.GetSummary())),
            "help" => Help(),
            "exit" or "quit" => CommandResult.Stop("Goodbye."),
            _ => CommandResult.From(
                $"Unknown command '{arguments[0]}'. Type 'help' for available commands."),
        };
    }

    private CommandResult ProcessApplication(IReadOnlyList<string> arguments)
    {
        if (arguments.Count != 4)
        {
            return CommandResult.From($"The apply command requires exactly three values. {Usage}");
        }

        if (!TryParseMoney(arguments[1], out var loanAmount))
        {
            return CommandResult.From(
                "Validation failed:",
                "- Loan amount must be a valid GBP monetary value.");
        }

        if (!TryParseMoney(arguments[2], out var assetValue))
        {
            return CommandResult.From(
                "Validation failed:",
                "- Asset value must be a valid GBP monetary value.");
        }

        if (!int.TryParse(
                arguments[3],
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var creditScore))
        {
            return CommandResult.From(
                "Validation failed:",
                "- Applicant credit score must be an integer between 1 and 999.");
        }

        var application = new LoanApplication(loanAmount, assetValue, creditScore);
        var result = _loanApplicationService.Submit(application);

        if (!result.IsValid)
        {
            return new CommandResult(
                ["Validation failed:", .. result.ValidationErrors.Select(error => $"- {error}")]);
        }

        var decision = result.Decision ??
            throw new InvalidOperationException("A valid application must contain a decision.");

        return CommandResult.From(
            $"Decision: {decision.Status}",
            $"Reason: {decision.Reason}",
            $"Application LTV: {decision.LoanToValuePercentage.ToString("F2", GbCulture)}%",
            FormatSummary(result.Summary));
    }

    private static bool TryParseMoney(string value, out decimal amount) =>
        decimal.TryParse(value, MoneyStyles, GbCulture, out amount);

    private static string FormatSummary(LoanOutcomeSummary summary) =>
        string.Join(
            Environment.NewLine,
            "Aggregate outcomes:",
            "Total number of applicants:",
            $"- Successful: {summary.SuccessfulApplicantCount}",
            $"- Declined: {summary.DeclinedApplicantCount}",
            $"Total value of loans written to date: {summary.TotalValueOfLoansWritten.ToString("C2", GbCulture)}",
            $"Mean average LTV across all applications: {summary.MeanLoanToValuePercentage.ToString("F2", GbCulture)}%");

    private static CommandResult Help() =>
        CommandResult.From(
            "Commands:",
            $"- {Usage}",
            "- stats",
            "- help",
            "- exit",
            "GBP values may include commas, decimals, and the £ symbol.");
}
