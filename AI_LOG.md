# AI Assistance Log

## Tool and scope

OpenAI Codex was used to review the supplied PDF, reason about boundaries,
design the solution, draft the .NET 10 source and tests, and perform a final
source-level review.

## Key prompts

1. Build a .NET 10 console app that runs a cancellable background worker, injects
   a command processor for console input, follows SOLID, and is testable.
2. Treat loan amount and secured asset value as validated monetary/decimal
   values, and validate credit score as an integer from 1 to 999.
3. Persist applicant counts by decision, total successful loan value, and mean
   LTV for all decisions for the POC lifetime.
4. Use an end-to-end test approach with an injectable test console and a real
   host created by `Host.CreateApplicationBuilder()`.
5. Read the attached specification and test successful, declined, and invalid
   input paths.

## Notable iterations and critical review

- The PDF uses strict upper comparisons for the below-£1m LTV bands. The
  implementation therefore treats exactly 60% as the 800-score band, exactly
  80% as the 900-score band, and exactly 90% as declined.
- The requested app-lifetime storage was first considered as static state. It
  was changed to state owned by a DI singleton: this has the same host lifetime
  but prevents state leaking between independently constructed test hosts.
- Loan-range breaches were kept as declined decisions, not validation errors,
  because the PDF lists them under business rules. Positive, correctly formed
  out-of-range amounts therefore still contribute to applicant count and mean
  LTV.
- Total loans written was interpreted as successful loan principal only. Mean
  LTV includes every valid decided application because the specification says
  "across all applications".
- The tests replace only the console adapter. They intentionally retain the real
  Generic Host, `BackgroundService`, command processor, validation, decision
  service, and singleton store.

## Verification note

The generated source, project references, decision boundaries, and expected test
outputs were reviewed. The generation environment did not contain a .NET SDK,
so compilation and test execution must be performed with the documented
`dotnet test` command on a machine with .NET 10 installed.
