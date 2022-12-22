using Commandir.Interfaces;
using Stubble.Core.Builders;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace Commandir.Commands;

/// <summary>
/// Encapsulates the parameters used by an Executor.
/// </summary>
internal sealed class ParameterContext : IParameterContext
{
    private readonly Dictionary<string, object?> _parameters;
    private readonly Stubble.Core.StubbleVisitorRenderer _renderer = new StubbleBuilder().Build();

    public string FormatParameters(string template)
    {
        return _renderer.Render(template, _parameters);   
    }

    /// <summary>
    /// Returns the value of the given parameter or null.
    /// </summary>
    public object? GetParameterValue(string parameterName)
    {
        _parameters.TryGetValue(parameterName, out object? parameterValue);
        return parameterValue;
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

    public ParameterContext(InvocationContext invocationContext, CommandWithData command)
    {
        // Ignore the case when looking up a parameter to make it easier on the user.
        Dictionary<string, object?> parameters = new(StringComparer.OrdinalIgnoreCase);

        // Add static parameters from parent commands, starting with the most distant parent.
        foreach(var parentCommand in command.GetParentCommands())
        {
            AddOrUpdateParameters(parameters, parentCommand.Data.Parameters);
        }

        // Add static parameters from this command.
        AddOrUpdateParameters(parameters, command.Data.Parameters);

        // Add dynamic parameters from this command invocation.
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