using System.CommandLine;

namespace Commandir.Commands;

internal sealed class CommandirCommand : Command
{
    public string? Action { get; set;}

    public Dictionary<string, object?> Parameters { get; set;} = new Dictionary<string, object?>();

    public CommandirCommand(string name, string? description, string? action, Dictionary<string, object?> parameters)
        : base(name, description)
    {
        Action = action;
        Parameters = parameters;
    }
}