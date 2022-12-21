using System.CommandLine;

namespace Commandir.Commands;

public sealed class CommandWithData : Command
{
    public CommandData Data { get; }

    public CommandWithData(CommandData data)
        : base(data.Name!, data.Description)
    {
        Data = data;
    }
}
