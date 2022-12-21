namespace Commandir.Commands;

internal static class CommandExtensions
{
    /// <summary>
    /// Returns the path for the command.
    /// Consider a command 'hello' with subcommand 'world'.
    /// The path for 'hello' is '/Commandir/hello/'.
    /// The path for 'world' is '/Commandir/hello/world'.
    /// </summary>
    internal static string GetPath(this CommandWithData command)
    {
        List<CommandWithData> commands = command.GetParentCommands();
        commands.Add(command);
        return "/" + string.Join("/", commands.Select(i => i.Name));
    }

    internal static List<CommandWithData> GetParentCommands(this CommandWithData command)
    {
        List<CommandWithData> parentCommands = new();
        command.GetParentCommands(parentCommands);
        
        // Reverse the list so the most distant parent is first.
        parentCommands.Reverse();
        
        return parentCommands;
    }

    private static void GetParentCommands(this CommandWithData command, List<CommandWithData> parentCommands)
    {
        foreach(var parent in command.Parents)
        {
            // The Parents collection of a System.CommandLine Command include Arguments and Options, so we need to exclude them. 
            if(parent is CommandWithData parentCommand)
            {
                parentCommands.Add(parentCommand);
                GetParentCommands(parentCommand, parentCommands);           
            }
        }
    }
}
