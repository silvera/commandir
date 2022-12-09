using Commandir.Interfaces;
using Commandir.Yaml;
using Microsoft.Extensions.Logging;
using Stubble.Core.Builders;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace Commandir.Commands;

// public sealed class StubbleParameterFormatter : IParameterFormatter
// {
//     private readonly Stubble.Core.StubbleVisitorRenderer _renderer;

//     public StubbleParameterFormatter()
//     {
//         _renderer = new StubbleBuilder().Build();
//     }
//     public string Format(string template, Dictionary<string, object?> parameters)
//     {
//         return _renderer.Render(template, parameters);
//     }
// }

public sealed class CommandExecutor2
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly  ICommandDataProvider<YamlCommandData> _commandDataProvider;

    private readonly Dictionary<string, Type> _executorTypes;

    public CommandExecutor2(ILoggerFactory loggerFactory, ICommandDataProvider<YamlCommandData> commandDataProvider)
    {
        _loggerFactory = loggerFactory;
        _commandDataProvider = commandDataProvider;
        _executorTypes = GetExecutorTypes();
    }

    private static Dictionary<string, Type> GetExecutorTypes()
    {
        var types = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        foreach (Type type in typeof(Program).Assembly.GetTypes())
        {
            if (typeof(IExecutor).IsAssignableFrom(type))
            {
                string typeName = type.FullName!;

                types.Add(typeName, type);
            }
        }
        return types;
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

    public Task<object?> ExecuteAsync(InvocationContext context)
    {
        var parseResult = context.ParseResult;

        var path = parseResult.CommandResult.Command.GetPath().Replace("/Commandir", string.Empty);
        
        var commandData = _commandDataProvider.GetCommandData(path);
        if(commandData == null)
            throw new Exception($"Failed to find command data data using path: {path}");

        Dictionary<string, object?> parameters = new Dictionary<string, object?>();

        // Add static parameters from parent commands.
        foreach(var parentCommand in GetParentCommands(commandData))
        {
            AddOrUpdateParameters(parameters, parentCommand.Parameters);
        }

        // Add static parameters from this command.
        AddOrUpdateParameters(parameters, commandData.Parameters);

        // Add dynamic parameters from this command invocation.
        var command = parseResult.CommandResult.Command;
        foreach(Argument argument in command.Arguments)
        {
            object? value = parseResult.GetValueForArgument(argument);
            AddOrUpdateParameter(parameters, argument.Name, value);
        }
        foreach(Option option in command.Options)
        {
            object? value = parseResult.GetValueForOption(option);
            AddOrUpdateParameter(parameters, option.Name, value);
        }

        var cancellationToken = context.GetCancellationToken();

        if(!_executorTypes.TryGetValue(commandData.Executor!, out Type? executorType))
            throw new Exception($"Failed to find executor: {commandData.Executor!}");

        var executor = Activator.CreateInstance(executorType) as IExecutor;
        if(executor == null)
            throw new Exception($"Failed to create executor: {executorType}");

        var executionContext = new ExecutionContext(_loggerFactory, cancellationToken, path, parameters);
        return executor.ExecuteAsync(executionContext);
    }
}

public sealed class ParameterContext : IParameterContext
{
    private readonly Stubble.Core.StubbleVisitorRenderer _renderer = new StubbleBuilder().Build();

    public Dictionary<string, object?> Parameters { get; }
    public string Format(string template)
    {
        return _renderer.Render(template, Parameters);   
    }

    public ParameterContext(Dictionary<string, object?> parameters)
    {
        Parameters = parameters;
    }
}

public sealed class ExecutionContext : IExecutionContext
{
    public ILoggerFactory LoggerFactory { get; }

    public CancellationToken CancellationToken { get; }

    // public Dictionary<string, object?> Parameters { get; }

    // public IParameterFormatter ParameterFormatter { get; }

    public string Path { get; }

    public IParameterContext ParameterContext { get; } 

    public ExecutionContext(ILoggerFactory loggerFactory, CancellationToken cancellationToken, string path, Dictionary<string, object?> parameters)
    {
        LoggerFactory = loggerFactory;
        CancellationToken = cancellationToken;
        Path = path;
        ParameterContext = new ParameterContext(parameters);
    }

    // public ExecutionContext(ILoggerFactory loggerFactory, IParameterFormatter parameterFormatter, CancellationToken cancellationToken, string path, Dictionary<string, object?> parameters)
    // {
    //     LoggerFactory = loggerFactory;
    //     //ParameterFormatter = parameterFormatter;
    //     CancellationToken = cancellationToken;
    //     Path = path;
    //     //Parameters = parameters;
    // }
}