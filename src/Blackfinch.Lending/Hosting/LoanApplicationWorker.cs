using Blackfinch.Lending.Commands;
using Blackfinch.Lending.Infrastructure;
using Microsoft.Extensions.Hosting;

namespace Blackfinch.Lending.Hosting;

public sealed class LoanApplicationWorker : BackgroundService
{
    private readonly IConsoleAdapter _console;
    private readonly ICommandProcessor _commandProcessor;
    private readonly IHostApplicationLifetime _applicationLifetime;

    public LoanApplicationWorker(
        IConsoleAdapter console,
        ICommandProcessor commandProcessor,
        IHostApplicationLifetime applicationLifetime)
    {
        _console = console;
        _commandProcessor = commandProcessor;
        _applicationLifetime = applicationLifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _console.WriteLine("Blackfinch Lending Platform");
        _console.WriteLine("Type 'help' for commands or 'exit' to stop.");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _console.Write("> ");
                var input = await _console.ReadLineAsync(stoppingToken);

                if (input is null)
                {
                    _applicationLifetime.StopApplication();
                    return;
                }

                var result = _commandProcessor.Process(input);
                foreach (var line in result.OutputLines)
                {
                    _console.WriteLine(line);
                }

                if (result.ShouldStop)
                {
                    _applicationLifetime.StopApplication();
                    return;
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal host shutdown (Ctrl+C, SIGTERM, or cancellation in a test).
        }
    }
}
