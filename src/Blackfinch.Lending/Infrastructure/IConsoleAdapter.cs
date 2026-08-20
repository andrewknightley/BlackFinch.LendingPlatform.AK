namespace Blackfinch.Lending.Infrastructure;

public interface IConsoleAdapter
{
    ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken);

    void Write(string value);

    void WriteLine(string value);
}
