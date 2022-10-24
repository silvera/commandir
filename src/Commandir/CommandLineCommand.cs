using Commandir.Core;
using System.CommandLine;

namespace Commandir;

internal sealed class CommandLineCommand : Command
{
    public CommandDefinition CommandDefinition { get; set; }

    public CommandLineCommand(CommandDefinition commandDefinition)
        : base(commandDefinition.Name!, commandDefinition.Description)
    {
        CommandDefinition = commandDefinition;
    }
}
