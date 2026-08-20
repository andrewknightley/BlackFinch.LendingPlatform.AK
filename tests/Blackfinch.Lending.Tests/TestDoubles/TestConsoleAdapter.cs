using System.Text;
using System.Threading.Channels;
using Blackfinch.Lending.Infrastructure;

namespace Blackfinch.Lending.Tests.TestDoubles;

internal sealed class TestConsoleAdapter : IConsoleAdapter
{
    private readonly Channel<string> _input = Channel.CreateUnbounded<string>();
    private readonly StringBuilder _output = new();
    private readonly object _outputLock = new();
    private readonly TaskCompletionSource<bool> _readStarted =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public TestConsoleAdapter(IEnumerable<string> inputLines)
    {
        foreach (var inputLine in inputLines)
        {
            if (!_input.Writer.TryWrite(inputLine))
            {
                throw new InvalidOperationException("Could not enqueue test console input.");
            }
        }
    }

    public string Output
    {
        get
        {
            lock (_outputLock)
            {
                return _output.ToString();
            }
        }
    }

    public Task ReadStarted => _readStarted.Task;

    public async ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken)
    {
        _readStarted.TrySetResult(true);
        return await _input.Reader.ReadAsync(cancellationToken);
    }

    public void Write(string value)
    {
        lock (_outputLock)
        {
            _output.Append(value);
        }
    }

    public void WriteLine(string value)
    {
        lock (_outputLock)
        {
            _output.AppendLine(value);
        }
    }
}
