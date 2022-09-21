namespace Commandir.Core;

public interface ICommand
{
    Task<CommandResult> ExecuteAsync(ICommandContext context);
}
