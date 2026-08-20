using Blackfinch.Lending.Application;
using Blackfinch.Lending.Domain;

namespace Blackfinch.Lending.Tests.Component;

public sealed class LoanApplicationServiceComponentTests
{
    [Theory]
    [MemberData(nameof(ValidApplicationDecisionCases))]
    public void GivenValidApplicationWhenSubmittedThenReturnsExpectedDecision(
        decimal loanAmount,
        decimal assetValue,
        int creditScore,
        LoanDecisionStatus expectedStatus,
        string expectedReason)
    {
        var sut = CreateSut();

        var result = sut.Submit(new LoanApplication(loanAmount, assetValue, creditScore));

        Assert.True(result.IsValid);
        Assert.Empty(result.ValidationErrors);
        var decision = Assert.IsType<LoanDecision>(result.Decision);
        Assert.Equal(expectedStatus, decision.Status);
        Assert.Contains(expectedReason, decision.Reason);
        Assert.Equal(loanAmount / assetValue * 100m, decision.LoanToValuePercentage);
        Assert.Equal(
            expectedStatus is LoanDecisionStatus.Successful ? 1 : 0,
            result.Summary.SuccessfulApplicantCount);
        Assert.Equal(
            expectedStatus is LoanDecisionStatus.Declined ? 1 : 0,
            result.Summary.DeclinedApplicantCount);
        Assert.Equal(
            expectedStatus is LoanDecisionStatus.Successful ? loanAmount : 0m,
            result.Summary.TotalValueOfLoansWritten);
        Assert.Equal(decision.LoanToValuePercentage, result.Summary.MeanLoanToValuePercentage);
    }

    [Theory]
    [MemberData(nameof(InvalidApplicationCases))]
    public void GivenInvalidApplicationWhenSubmittedThenValidationErrorsAndNoOutcomeRecorded(
        decimal loanAmount,
        decimal assetValue,
        int creditScore,
        string expectedError)
    {
        var sut = CreateSut();

        var result = sut.Submit(new LoanApplication(loanAmount, assetValue, creditScore));

        Assert.False(result.IsValid);
        Assert.Null(result.Decision);
        Assert.Contains(expectedError, result.ValidationErrors);
        Assert.Equal(LoanOutcomeSummary.Empty, result.Summary);
        Assert.Equal(LoanOutcomeSummary.Empty, sut.GetSummary());
    }

    [Fact]
    public void GivenApplicationWithMultipleInvalidFieldsWhenSubmittedThenReturnsAllValidationErrors()
    {
        var sut = CreateSut();

        var result = sut.Submit(new LoanApplication(0m, -1m, 1_000));

        Assert.False(result.IsValid);
        Assert.Null(result.Decision);
        Assert.Collection(
            result.ValidationErrors,
            error => Assert.Equal("Loan amount must be greater than £0.00.", error),
            error => Assert.Equal("Asset value must be greater than £0.00.", error),
            error => Assert.Equal(
                "Applicant credit score must be an integer between 1 and 999.",
                error));
        Assert.Equal(LoanOutcomeSummary.Empty, result.Summary);
    }

    [Fact]
    public void GivenExistingSummaryWhenLaterApplicationIsInvalidThenSummaryUnchanged()
    {
        var sut = CreateSut();
        var accepted = sut.Submit(new LoanApplication(500_000m, 1_000_000m, 750));

        var invalid = sut.Submit(new LoanApplication(0m, 1_000_000m, 750));

        Assert.False(invalid.IsValid);
        Assert.Equal(accepted.Summary, invalid.Summary);
        Assert.Equal(accepted.Summary, sut.GetSummary());
    }

    [Fact]
    public void GivenMultipleValidApplicationsWhenSubmittedThenAggregatesSuccessfulAndDeclinedApplications()
    {
        var sut = CreateSut();

        sut.Submit(new LoanApplication(500_000m, 1_000_000m, 750)); // 50%, successful
        sut.Submit(new LoanApplication(600_000m, 750_000m, 899));   // 80%, declined
        var finalResult = sut.Submit(
            new LoanApplication(1_200_000m, 2_000_000m, 950));      // 60%, successful

        Assert.Equal(2, finalResult.Summary.SuccessfulApplicantCount);
        Assert.Equal(1, finalResult.Summary.DeclinedApplicantCount);
        Assert.Equal(1_700_000m, finalResult.Summary.TotalValueOfLoansWritten);
        Assert.Equal((50m + 80m + 60m) / 3m, finalResult.Summary.MeanLoanToValuePercentage);
        Assert.Equal(finalResult.Summary, sut.GetSummary());
    }

    [Fact]
    public void GivenNullApplicationWhenSubmittedThenThrowsArgumentNullException()
    {
        var sut = CreateSut();

        Assert.Throws<ArgumentNullException>(() => sut.Submit(null!));
        Assert.Equal(LoanOutcomeSummary.Empty, sut.GetSummary());
    }

    public static TheoryData<decimal, decimal, int, LoanDecisionStatus, string>
        ValidApplicationDecisionCases =>
        new()
        {
            // General loan limits.
            { 99_999.99m, 200_000m, 999, LoanDecisionStatus.Declined, "between £100,000 and £1,500,000 inclusive" },
            { 100_000m, 200_000m, 1, LoanDecisionStatus.Declined, "credit score of at least 750" },
            { 100_000m, 200_000m, 750, LoanDecisionStatus.Successful, "meets the lending criteria" },
            { 1_500_000m, 2_500_000m, 950, LoanDecisionStatus.Successful, "meets the lending criteria" },
            { 1_500_000.01m, 3_000_000m, 999, LoanDecisionStatus.Declined, "between £100,000 and £1,500,000 inclusive" },

            // Loans of £1m or more: LTV <= 60% and credit score >= 950.
            { 1_000_000m, 2_000_000m, 950, LoanDecisionStatus.Successful, "meets the lending criteria" },
            { 1_200_000m, 2_000_000m, 950, LoanDecisionStatus.Successful, "meets the lending criteria" },
            { 1_200_000m, 2_000_000m, 949, LoanDecisionStatus.Declined, "credit score of at least 950" },
            { 1_200_000m, 1_999_999m, 999, LoanDecisionStatus.Declined, "LTV of 60% or less" },

            // Loans below £1m: each LTV band and its exact boundary.
            { 500_000m, 1_000_000m, 750, LoanDecisionStatus.Successful, "meets the lending criteria" },
            { 500_000m, 1_000_000m, 749, LoanDecisionStatus.Declined, "credit score of at least 750" },
            { 600_000m, 1_000_000m, 800, LoanDecisionStatus.Successful, "meets the lending criteria" },
            { 600_000m, 1_000_000m, 799, LoanDecisionStatus.Declined, "credit score of at least 800" },
            { 799_999m, 1_000_000m, 800, LoanDecisionStatus.Successful, "meets the lending criteria" },
            { 800_000m, 1_000_000m, 900, LoanDecisionStatus.Successful, "meets the lending criteria" },
            { 800_000m, 1_000_000m, 899, LoanDecisionStatus.Declined, "credit score of at least 900" },
            { 899_999m, 1_000_000m, 900, LoanDecisionStatus.Successful, "meets the lending criteria" },
            { 900_000m, 1_000_000m, 999, LoanDecisionStatus.Declined, "LTV below 90%" },
        };

    public static TheoryData<decimal, decimal, int, string> InvalidApplicationCases =>
        new()
        {
            { 0m, 1_000_000m, 750, "Loan amount must be greater than £0.00." },
            { -0.01m, 1_000_000m, 750, "Loan amount must be greater than £0.00." },
            { 100_000.001m, 1_000_000m, 750, "Loan amount must have no more than two decimal places." },
            { 100_000m, 0m, 750, "Asset value must be greater than £0.00." },
            { 100_000m, -0.01m, 750, "Asset value must be greater than £0.00." },
            { 100_000m, 1_000_000.001m, 750, "Asset value must have no more than two decimal places." },
            { 100_000m, 1_000_000m, 0, "Applicant credit score must be an integer between 1 and 999." },
            { 100_000m, 1_000_000m, 1_000, "Applicant credit score must be an integer between 1 and 999." },
        };

    private static LoanApplicationService CreateSut() =>
        new(
            new LoanApplicationValidator(),
            new LoanDecisionService(),
            new InMemoryLoanOutcomeStore());
}