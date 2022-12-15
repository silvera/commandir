using Commandir.Interfaces;
using Commandir.Yaml;
using Stubble.Core.Builders;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace Commandir.Commands;

public sealed class ParameterContext : IParameterContext
{
    private readonly Dictionary<string, object?> _parameters;
    private readonly Stubble.Core.StubbleVisitorRenderer _renderer = new StubbleBuilder().Build();

    public string FormatParameters(string template)
    {
        return _renderer.Render(template, _parameters);   
    }

    public object? GetParameterValue(string parameterName)
    {
        _parameters.TryGetValue(parameterName, out object? parameterValue);
        return parameterValue;
    }

    private static List<YamlCommandData> GetParentCommands(YamlCommandData commandData)
    {
        var components = new List<YamlCommandData>();
        var current = commandData.Parent;
        while(current != null)
        {
            components.Add(current);
            current = current.Parent;
        }

        components.Reverse();
        return components;
    }

    private static void AddOrUpdateParameters(Dictionary<string, object?> dst, Dictionary<string, object?> src)
    {
        foreach(var pair in src)
        {
            AddOrUpdateParameter(dst, pair.Key, pair.Value);
        }
    }

    private static void AddOrUpdateParameter(Dictionary<string, object?> dst, string name, object? value)
    {
        if(value != null)
            dst[name] = value;
    }

    public ParameterContext(InvocationContext invocationContext, YamlCommandData commandData)
    {
        Dictionary<string, object?> parameters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        // Add static parameters from parent commands.
        foreach(var parentCommand in GetParentCommands(commandData))
        {
            AddOrUpdateParameters(parameters, parentCommand.Parameters);
        }

        // Add static parameters from this command.
        AddOrUpdateParameters(parameters, commandData.Parameters);

        // Add dynamic parameters from this command invocation.
        var command = invocationContext.ParseResult.CommandResult.Command;
        foreach(Argument argument in command.Arguments)
        {
            object? value = invocationContext.ParseResult.GetValueForArgument(argument);
            AddOrUpdateParameter(parameters, argument.Name, value);
        }
        foreach(Option option in command.Options)
        {
            object? value = invocationContext.ParseResult.GetValueForOption(option);
            AddOrUpdateParameter(parameters, option.Name, value);
        }

        _parameters = parameters;
    }
}