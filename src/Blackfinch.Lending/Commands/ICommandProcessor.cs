namespace Blackfinch.Lending.Commands;

public interface ICommandProcessor
{
    CommandResult Process(string? input);
}
