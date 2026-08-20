namespace Blackfinch.Lending.Domain;

public sealed record LoanApplication(
    decimal LoanAmount,
    decimal AssetValue,
    int CreditScore);
