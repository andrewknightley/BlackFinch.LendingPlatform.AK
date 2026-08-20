using Blackfinch.Lending.Infrastructure;
using Blackfinch.Lending.Tests.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Blackfinch.Lending.Tests.E2E;

public sealed class LendingPlatformE2ETests
{
    [Fact]
    public async Task GivenHostStartedWhenStopRequestedThenWorkerRemainsActiveUntilHostStops()
    {
        var testConsole = new TestConsoleAdapter(Array.Empty<string>());
        using var host = CreateHost(testConsole);
        var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

        await host.StartAsync();
        await testConsole.ReadStarted.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.False(lifetime.ApplicationStopping.IsCancellationRequested);

        await host.StopAsync();

        Assert.True(lifetime.ApplicationStopped.IsCancellationRequested);
    }

    [Fact]
    public async Task GivenValidApplicationWhenApplicantMeetsCriteriaThenDecisionSuccessfulAndAggregatesUpdated()
    {
        var output = await RunScenarioAsync(
            "apply 500000 1000000 750",
            "exit");

        Assert.Contains("Decision: Successful", output);
        Assert.Contains("Application LTV: 50.00%", output);
        Assert.Contains("- Successful: 1", output);
        Assert.Contains("- Declined: 0", output);
        Assert.Contains("Total value of loans written to date: £500,000.00", output);
        Assert.Contains("Mean average LTV across all applications: 50.00%", output);
    }

    [Fact]
    public async Task GivenApplicationWithLowScoreWhenApplicantDoesNotMeetCriteriaThenDecisionDeclinedAndAggregatesUpdated()
    {
        var output = await RunScenarioAsync(
            "apply 500000 1000000 749",
            "exit");

        Assert.Contains("Decision: Declined", output);
        Assert.Contains("- Successful: 0", output);
        Assert.Contains("- Declined: 1", output);
        Assert.Contains("Total value of loans written to date: £0.00", output);
        Assert.Contains("Mean average LTV across all applications: 50.00%", output);
    }

    [Fact]
    public async Task GivenMultipleValidApplicationsWhenStatsRequestedThenAggregatesIncludeAllDecisionsDuringHostLifetime()
    {
        var output = await RunScenarioAsync(
            "apply 500000 1000000 750",
            "apply 600000 750000 899",
            "stats",
            "exit");

        Assert.Contains("- Successful: 1", output);
        Assert.Contains("- Declined: 1", output);
        Assert.Contains("Total value of loans written to date: £500,000.00", output);
        Assert.Contains("Mean average LTV across all applications: 65.00%", output);
    }

    [Theory]
    [InlineData(
        "apply not-money 1000000 750",
        "Loan amount must be a valid GBP monetary value.")]
    [InlineData(
        "apply 100000 0 750",
        "Asset value must be greater than £0.00.")]
    [InlineData(
        "apply 100000 1000000 1000",
        "Applicant credit score must be an integer between 1 and 999.")]
    [InlineData(
        "apply 100000.001 1000000 750",
        "Loan amount must have no more than two decimal places.")]
    public async Task GivenInvalidInputWhenSubmittedThenValidationFailsAndNoOutcomeRecorded(
        string command,
        string expectedError)
    {
        var output = await RunScenarioAsync(command, "stats", "exit");

        Assert.Contains("Validation failed:", output);
        Assert.Contains(expectedError, output);
        Assert.DoesNotContain("Decision:", output);
        Assert.Contains("- Successful: 0", output);
        Assert.Contains("- Declined: 0", output);
        Assert.Contains("Total value of loans written to date: £0.00", output);
        Assert.Contains("Mean average LTV across all applications: 0.00%", output);
    }

    [Theory]
    [InlineData("apply 99999 200000 999", "Declined")]
    [InlineData("apply 1500000 2500000 950", "Successful")]
    [InlineData("apply 1500000.01 3000000 999", "Declined")]
    [InlineData("apply 1200000 2000000 950", "Successful")]
    [InlineData("apply 600000 1000000 800", "Successful")]
    [InlineData("apply 800000 1000000 900", "Successful")]
    [InlineData("apply 900000 1000000 999", "Declined")]
    public async Task GivenVariousBoundaryInputsWhenEvaluatedThenDecisionMatchesSpecification(
        string command,
        string expectedStatus)
    {
        var output = await RunScenarioAsync(command, "exit");

        Assert.Contains($"Decision: {expectedStatus}", output);
    }

    private static async Task<string> RunScenarioAsync(params string[] commands)
    {
        var testConsole = new TestConsoleAdapter(commands);
        using var host = CreateHost(testConsole);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await host.RunAsync(timeout.Token);

        Assert.False(
            timeout.IsCancellationRequested,
            "The real host did not stop after the test console supplied the exit command.");

        return testConsole.Output;
    }

    private static IHost CreateHost(TestConsoleAdapter testConsole)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Services.AddLendingPlatform();
        builder.Services.RemoveAll<IConsoleAdapter>();
        builder.Services.AddSingleton<IConsoleAdapter>(testConsole);

        return builder.Build();
    }
}