namespace Blackfinch.Lending.Commands;

public sealed record CommandResult(IReadOnlyList<string> OutputLines, bool ShouldStop = false)
{
    public static CommandResult From(params string[] lines) => new(lines);

    public static CommandResult Stop(params string[] lines) => new(lines, true);
}
