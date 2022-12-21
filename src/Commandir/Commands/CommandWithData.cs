using System.CommandLine;

namespace Commandir.Commands;

/// <summary>
/// A System.CommandLine command with its associated CommandData.
/// </summary>
internal sealed class CommandWithData : Command
{
    /// <summary>
    /// The command data.
    /// </summary>
    public CommandData Data { get; }

    public CommandWithData(CommandData data)
        : base(data.Name!, data.Description)
    {
        Data = data;
    }
}
