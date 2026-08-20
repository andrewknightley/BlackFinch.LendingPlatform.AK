namespace Blackfinch.Lending.Infrastructure;

public sealed class SystemConsoleAdapter : IConsoleAdapter
{
    public ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken) =>
        Console.In.ReadLineAsync(cancellationToken);

    public void Write(string value) => Console.Write(value);

    public void WriteLine(string value) => Console.WriteLine(value);
}
