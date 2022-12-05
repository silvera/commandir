using Commandir.Commands;
using Commandir.Interfaces;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace Commandir.Services;

public sealed class CommandLineCommandData : IDynamicCommandData
{
    public string Path {get; }

    public Dictionary<string, object?> Parameters { get; }

    public CommandLineCommandData(string path, Dictionary<string, object?> parameters)
    {
        Path = path;
        Parameters = parameters;
    }
}

public sealed class CommandLineCommandDataProvider : IDynamicCommandDataProvider, ICancellationTokenProvider
{
    private readonly InvocationContext _invocationContext;
    public CommandLineCommandDataProvider(InvocationContext invocationContext)
    {
        _invocationContext = invocationContext;
    }

    public CancellationToken GetCancellationToken()
    {
        return _invocationContext.GetCancellationToken();
    }

    public IDynamicCommandData GetCommandData()
    {
        var parseResult = _invocationContext.ParseResult;
        var command = parseResult.CommandResult.Command;

        Dictionary<string, object?> parameters = new Dictionary<string, object?>();
        foreach(Argument argument in command.Arguments)
        {
            object? value = parseResult.GetValueForArgument(argument);
            if(value != null)
            {
                parameters[argument.Name] =  value;
            }
        }

        foreach(Option option in command.Options)
        {
            object? value = parseResult.GetValueForOption(option);
            if(value != null)
            {
                parameters[option.Name] = value;
            } 
        }

        string path = command.GetPath();
        path = path.Replace("/Commandir", string.Empty);
        return new CommandLineCommandData(path, parameters);         
    }
}