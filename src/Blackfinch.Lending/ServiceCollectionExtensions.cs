using Blackfinch.Lending.Application;
using Blackfinch.Lending.Commands;
using Blackfinch.Lending.Domain;
using Blackfinch.Lending.Hosting;
using Blackfinch.Lending.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Blackfinch.Lending;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLendingPlatform(this IServiceCollection services)
    {
        services.AddSingleton<ILoanDecisionService, LoanDecisionService>();
        services.AddSingleton<ILoanApplicationValidator, LoanApplicationValidator>();

        // This singleton owns in-memory state for exactly one application/host lifetime.
        services.AddSingleton<ILoanOutcomeStore, InMemoryLoanOutcomeStore>();

        services.AddSingleton<ILoanApplicationService, LoanApplicationService>();
        services.AddSingleton<ICommandProcessor, CommandProcessor>();
        services.AddSingleton<IConsoleAdapter, SystemConsoleAdapter>();
        services.AddHostedService<LoanApplicationWorker>();

        return services;
    }
}
