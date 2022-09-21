namespace Commandir.Core;

public interface ICommand
{
    Task<CommandResult> ExecuteAsync(CommandContext context);
}
