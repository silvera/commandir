namespace Commandir;

using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using System.CommandLine.Invocation;

using Commandir.Core;

public sealed class CommandContext : ICommandContext
{
    public IServiceProvider Services { get; }

    private readonly Dictionary<string, object?> _parameters = new Dictionary<string, object?>();
    public IReadOnlyDictionary<string, object?> Parameters => _parameters;
    
    public CommandContext(IServiceProvider services)
    {
        Services = services;

        InvocationContext invocationContext = services.GetRequiredService<InvocationContext>();
        if(invocationContext == null)
            throw new Exception("InvocationContext is null.");

        CommandLineCommand? command = invocationContext.ParseResult.CommandResult.Command as CommandLineCommand;
        if(command == null)
            throw new Exception("Command is null.");

        // Add command parameters.
        foreach(var parameterPair in command.CommandData.Parameters)
        {
            _parameters[parameterPair.Key] = parameterPair.Value;
        }

        // Add command line parameters after command parameters so they can override the parameters.
        foreach(Argument argument in command.Arguments)
        {
            object? value = invocationContext.ParseResult.GetValueForArgument(argument);
            if(value != null)
                _parameters[argument.Name] = value;
        }

        foreach(Option option in command.Options)
        {
            object? value = invocationContext.ParseResult.GetValueForOption(option);
            if(value != null)
            _parameters[option.Name] = value;  
        }
    }
}