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

The solution has two deliberately different test projects.

`Blackfinch.Lending.ComponentTests` exercises `LoanApplicationService` with the
real validator, decision service, and in-memory outcome store. It covers:

- successful and declined paths for every lending-rule branch;
- the exact £100,000, £1 million, and £1.5 million loan boundaries;
- the exact 60%, 80%, and 90% LTV boundaries;
- accepted and rejected credit-score boundaries for every LTV band;
- every validation rule, including multiple simultaneous errors;
- aggregate updates and proof that invalid applications do not change them.

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

## Continuous integration

`.github/workflows/dotnet.yml` runs for pushes and pull requests to `main`, as
well as manual dispatches. It:

1. installs the latest patched .NET 10 SDK;
2. restores and builds the complete solution in Release configuration;
3. runs both test projects;
4. creates a framework-dependent, portable publish output without a native app
   host;
5. uploads that publish directory as a downloadable workflow artifact retained
   for 14 days.

The uploaded artifact can be started on a machine with the .NET 10 runtime by
running:

```powershell
dotnet Blackfinch.Lending.dll
```

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

## AI Prompts Used During Development

1. I require a dotnet console app that should run a backgroundworker injected as a HostedService so it stays running until it is cancelled.
In turn this service should have a commandprocessor injected to handle the console user input args.
We should use SOLID principles and make the components testable.
It should use dotnet 10.

In respect to the inputs listed in the spec "Loan Amount" and Asset Value The Loan Is Secured Against" should be monetary/decimal values and should be validated.
Applicants credit score should be validated as an integer value between 1 and 999

The outputs "Total Number Of Applicants (grouped by success status)", "Total Value Of Loans Written" and "Mean Average Load To Value Across All Applicants" are aggregated values for all loan decisions made. These values should be persisted for the lifetime of the App for our POC. To avoid integration with a db I would suggest a singleton with static storage for our aggregated loan outcomes.

In terms of testing we will require a test project using an e2e testing approach that uses a test console so we can inject the user inputs.
We should use a real Host as our system under test so be sure to use var builder = Host.CreateApplicationBuilder(); and inject the appropriate instances into our ServiceCollection.

I would like to see tests for Successful, Declined responses and tests for input validation failures.

We can review again after that.

The requirements for this application are in the attached spec.

2. Can you rename the tests in the E2E folder to use a Given{Action}When{Conditions}Then{Outcome} naming convention please?

3. I would like a new component test project and some tests on LoanApplicationService that test all valid and invalid outcome paths for confidence in the loan decision logic Also I would like a github actions workflow to build, tests and publish a release artifact for this service

4. I have a gh worklfow file can we add a trigger so it runs on any branch push for build and test and onlypublishes if the branch is main?
