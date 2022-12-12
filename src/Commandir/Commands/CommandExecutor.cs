using Commandir.Interfaces;
using Commandir.Yaml;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace Commandir.Commands;

public interface ICommandExecutionResult
{
    bool HasResult { get; }
}

public sealed class EmptyCommandExecutionResult : ICommandExecutionResult
{
    public bool HasResult => false;
}

public sealed class SingleCommmandExecutionResult : ICommandExecutionResult
{
    public object? Value { get; }

    public SingleCommmandExecutionResult(object? value )
    {
        Value = value;
    }

    public bool HasResult => Value is not null;
}

public sealed class MultipleCommandExecutionResult : ICommandExecutionResult
{
    public List<object?>? Values { get; }

    public MultipleCommandExecutionResult(List<object?> values)
    {
        Values = values;
    }

    public bool HasResult => Values is not null;
}


public sealed class CommandExecutor
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly  ICommandDataProvider<YamlCommandData> _commandDataProvider;

    private readonly Dictionary<string, Type> _executorTypes;

    public CommandExecutor(ILoggerFactory loggerFactory, ICommandDataProvider<YamlCommandData> commandDataProvider)
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

    private static Dictionary<string, object?> ResolveParameters(InvocationContext context, YamlCommandData commandData)
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
        var command = context.ParseResult.CommandResult.Command;
        foreach(Argument argument in command.Arguments)
        {
            object? value = context.ParseResult.GetValueForArgument(argument);
            AddOrUpdateParameter(parameters, argument.Name, value);
        }
        foreach(Option option in command.Options)
        {
            object? value = context.ParseResult.GetValueForOption(option);
            AddOrUpdateParameter(parameters, option.Name, value);
        }

        return parameters;
    }

    public Task<object?> ExecuteAsync(InvocationContext context, YamlCommandData commandData, string commandPath)
    {
        // Resolve parameters.
        var parameters = ResolveParameters(context, commandData);

        if(commandData.Executor is null)
            throw new Exception($"Executor is null");

        if(!_executorTypes.TryGetValue(commandData.Executor!, out Type? executorType))
            throw new Exception($"Failed to find executor: {commandData.Executor!}");

        var executor = Activator.CreateInstance(executorType) as IExecutor;
        if(executor == null)
            throw new Exception($"Failed to create executor: {executorType}");

        var executionContext = new Commandir.Interfaces.ExecutionContext(_loggerFactory, context.GetCancellationToken(), commandPath, parameters);
        return executor.ExecuteAsync(executionContext);
    }

    public async Task<ICommandExecutionResult> ExecuteAsync(InvocationContext context)
    {
        var commandPath = PathProvider
            .GetPath(context.ParseResult.CommandResult.Command, 
                    data => data.Name, 
                    data => data.Parents.Where(parent => parent is Command).Cast<Command>())
            .Replace("/Commandir", string.Empty);

        var commandData = _commandDataProvider.GetCommandData(commandPath);
        if(commandData == null)
            throw new Exception($"Failed to find command data data using path: {commandPath}");

        if(commandData.Commands.Count == 0)
        {
            // This is a leaf command.
            var result = await ExecuteAsync(context, commandData, commandPath); 
            return new SingleCommmandExecutionResult(result);
        }
        else
        {
            // This is a non-leaf (internal) command.
            
            // Resolve parameters as if we were executing the command directly.
            var parameters = ResolveParameters(context, commandData);

            // We require a 'recurse' parameter entry to decide if we should execute child commands (recursively). 
            if(!parameters.TryGetValue("recurse", out object? recurseObj))
                return new EmptyCommandExecutionResult();

            bool recurse = Convert.ToBoolean(recurseObj);
            if(!recurse)
                return new EmptyCommandExecutionResult();

            // Decide if child commands should be executed serially (the default) or in parallel.
            bool parallel = false;
            if(parameters.TryGetValue("parallel", out object? parallelObj))
            {
                parallel = Convert.ToBoolean(parallelObj);
            }

            List<Task<object?>> subCommandTasks = new List<Task<object?>>();
            foreach(var subCommandData in commandData.Commands)
            {
                string subCommandPath = PathProvider
                    .GetPath(subCommandData, 
                            data => data.Name!, 
                            data => data.Commands);
                var subCommandTask = ExecuteAsync(context, subCommandData, subCommandPath);
                subCommandTasks.Add(subCommandTask);
            }

            if(parallel)
            {
                await Task.WhenAll(subCommandTasks.ToArray());
                var results = new List<object?>();
                foreach(var subCommandTask in subCommandTasks)
                {
                    var result = await subCommandTask;
                    results.Add(result);
                }

                return new MultipleCommandExecutionResult(results);
            }
            else
            {
                var results = new List<object?>();
                foreach(var subCommandTask in subCommandTasks)
                {
                    var result = await subCommandTask;
                    results.Add(result);
                }

                return new MultipleCommandExecutionResult(results);
            }
        }
    }
}