# Blackfinch Lending Platform

A .NET 10 console proof of concept that applies the supplied lending rules and
keeps aggregate outcomes in memory for the lifetime of the application.

## Run it

Prerequisite: the .NET 10 SDK.

```powershell
dotnet restore
dotnet test
dotnet run --project src/Blackfinch.Lending
```

The app remains active until `exit`, `quit`, Ctrl+C, SIGTERM, or end-of-input.

## Commands

```text
apply <loan-amount> <asset-value> <credit-score>
stats
help
exit
```

Examples:

```text
apply 500000 1000000 750
apply £600,000.00 £750,000.00 899
stats
exit
```

Loan amount and asset value accept GBP decimal values, with optional commas and
the `£` symbol. Both must be positive and have no more than two decimal places.
Credit score must be an integer from 1 to 999 inclusive.

## Design

- `LoanApplicationWorker` is a cancellable `BackgroundService` and owns only the
  console loop and host shutdown behaviour.
- `CommandProcessor` parses commands and formats responses.
- `LoanApplicationValidator` validates application inputs.
- `LoanDecisionService` contains the lending rules without console or storage
  dependencies.
- `LoanApplicationService` coordinates validation, decision-making, and
  recording.
- `InMemoryLoanOutcomeStore` is registered as a singleton, so its state lasts
  for one real host/application lifetime.
- `IConsoleAdapter` makes console I/O replaceable. The end-to-end tests replace
  only this boundary and run the actual Generic Host, worker, command processor,
  domain services, and store.

The aggregate store deliberately uses singleton **instance** state rather than
static global state. It provides the required POC lifetime while keeping each
host isolated, allowing tests to run independently and safely in parallel.

## Aggregate semantics and assumptions

- A valid application is recorded whether its decision is successful or
  declined.
- A validation failure is not a loan decision and does not change aggregates.
- "Total value of loans written" is the sum of loan amounts for successful
  applications only.
- Mean LTV is the arithmetic mean of each valid application's LTV, including
  successful and declined applications.
- The £100,000 and £1,500,000 loan limits are decision rules, not validation
  limits. A positive monetary value outside the range is valid input and receives
  a declined decision.
- Boundary wording is applied literally: below £1m, exactly 60% LTV requires a
  score of at least 800, exactly 80% requires at least 900, and exactly 90% is
  declined. Exactly £1m uses the high-value-loan rules.
- In-memory aggregates reset when the host exits. A production implementation
  can replace `ILoanOutcomeStore` without changing the decision or command
  components.

## Tests

`Blackfinch.Lending.Tests` contains host-level end-to-end tests covering:

- successful decisions;
- declined decisions;
- aggregates across multiple applications;
- invalid money, non-positive values, excessive decimal precision, and invalid
  credit scores;
- the significant loan, LTV, and credit-score boundaries.

Each test creates the system under test with
`var builder = Host.CreateApplicationBuilder();`, injects a channel-backed test
console, supplies commands, and lets `exit` shut the real host down gracefully.

## Production follow-ups

- Persist outcomes transactionally in a database and define recovery/idempotency
  behaviour.
- Replace `int` aggregate counters if application volume could exceed their
  range.
- Add structured logging, metrics, health checks, and correlation identifiers.
- Confirm currency, rounding, and mean-LTV definitions with the product owner.
- Add authentication and an API/message boundary if the process becomes a
  service rather than an interactive console POC.

See [AI_LOG.md](AI_LOG.md) for the requested AI assistance record.
