using Commandir.Interfaces;
using Commandir.Yaml;
using Microsoft.Extensions.Logging;
using System.CommandLine.Invocation;

namespace Commandir.Commands;

// Marker interface for command execution result types.
public interface ICommandExecutionResult
{
}

// Represents a failed command execution.
public sealed class FailedCommandExecution : ICommandExecutionResult
{   
    public string Error { get; }
    public FailedCommandExecution(string error)
    {
        Error = error;
    }
}

// Represents a successful command execution.
public sealed class SuccessfulCommandExecution : ICommandExecutionResult
{
    public IEnumerable<object?> Results { get; }
    public SuccessfulCommandExecution(IEnumerable<object?> results)
    {
        Results = results;
    }
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

    // private static List<YamlCommandData> GetParentCommands(YamlCommandData commandData)
    // {
    //     var components = new List<YamlCommandData>();
    //     var current = commandData.Parent;
    //     while(current != null)
    //     {
    //         components.Add(current);
    //         current = current.Parent;
    //     }

    //     components.Reverse();
    //     return components;
    // }

    // private static void AddOrUpdateParameters(Dictionary<string, object?> dst, Dictionary<string, object?> src)
    // {
    //     foreach(var pair in src)
    //     {
    //         AddOrUpdateParameter(dst, pair.Key, pair.Value);
    //     }
    // }

    // private static void AddOrUpdateParameter(Dictionary<string, object?> dst, string name, object? value)
    // {
    //     if(value != null)
    //         dst[name] = value;
    // }

    // private ParameterContext GetParameterContext(InvocationContext invocationContext, YamlCommandData commandData)
    // {
    //     Dictionary<string, object?> parameters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

    //     // Add static parameters from parent commands.
    //     foreach(var parentCommand in GetParentCommands(commandData))
    //     {
    //         AddOrUpdateParameters(parameters, parentCommand.Parameters);
    //     }

    //     // Add static parameters from this command.
    //     AddOrUpdateParameters(parameters, commandData.Parameters);

    //     // Add dynamic parameters from this command invocation.
    //     var command = invocationContext.ParseResult.CommandResult.Command;
    //     foreach(Argument argument in command.Arguments)
    //     {
    //         object? value = invocationContext.ParseResult.GetValueForArgument(argument);
    //         AddOrUpdateParameter(parameters, argument.Name, value);
    //     }
    //     foreach(Option option in command.Options)
    //     {
    //         object? value = invocationContext.ParseResult.GetValueForOption(option);
    //         AddOrUpdateParameter(parameters, option.Name, value);
    //     }

    //     return new ParameterContext(parameters);
    // }
    private sealed class Executable
    {
        private readonly IExecutor _executor;
        private readonly IExecutionContext _executionContext;
        public Executable(IExecutor executor, IExecutionContext executionContext)
        {
            _executor = executor;
            _executionContext = executionContext;
        }

        public string Path => _executionContext.Path;

        public Task<object?> ExecuteAsync()
        {
            return _executor.ExecuteAsync(_executionContext);
        }
    }

    private Executable GetExecutable(InvocationContext context, YamlCommandData commandData, ParameterContext parameterContext)
    {
        if(commandData.Executor is null)
            throw new Exception($"Executor is null");

        if(!_executorTypes.TryGetValue(commandData.Executor!, out Type? executorType))
            throw new Exception($"Failed to find executor: {commandData.Executor!}");

        var executor = Activator.CreateInstance(executorType) as IExecutor;
        if(executor == null)
            throw new Exception($"Failed to create executor: {executorType}");

        var executionContext = new Commandir.Interfaces.ExecutionContext(_loggerFactory, context.GetCancellationToken(), commandData.Path!, parameterContext);
        return new Executable(executor, executionContext);
    }   

    private static ICommandExecutionResult? ValidateParseResult(InvocationContext invocationContext)
    {
        var parseErrors = invocationContext.ParseResult.Errors; 
        
        // Check for unexpected parse errors
        if(parseErrors.Count > 0)
        {
            if(parseErrors.Count > 1)
            {
                // There is more than one parse error; return the last error.
                return new FailedCommandExecution(parseErrors.Last().Message);
            }
            else
            {
                // Ensure the one parse error is the expected error.
                var error = parseErrors.First();
                if(error.Message != "Required command was not provided.")
                    return new FailedCommandExecution(error.Message);
            }
        }

        return null;
    }

    private void GetExecutables(InvocationContext invocationContext, YamlCommandData commandData, List<Executable> executables)
    {   
        bool isLeafCommand = commandData.Commands.Count == 0;

        // Internal commands are not executable by default but leaf commands are.
        bool isExecutable = isLeafCommand;

        var parameterContext = new ParameterContext(invocationContext, commandData);
        object? executableObj = parameterContext.GetParameterValue("executable");
        if(executableObj is not null)
        {
            isExecutable = Convert.ToBoolean(executableObj);
        }

        if(isExecutable)
        {
            if(isLeafCommand)
            {
                var executable = GetExecutable(invocationContext, commandData, parameterContext);
                executables.Add(executable);
            }
            foreach(var subCommandData in commandData.Commands)
            {
                GetExecutables(invocationContext, subCommandData, executables);
            }
        }
    }

    public async Task<ICommandExecutionResult> ExecuteAsync(InvocationContext invocationContext)
    {
        // Manually surface any parse errors that should prevent command execution.
        // This is required because we're using Middlware to bypass the standard command execution pipeline.
        // This lets us handle invocations for internal commands that would normally result in a "Required command was not provided." error.
        var validationResult = ValidateParseResult(invocationContext);
        if(validationResult != null)
            return validationResult;

        string commandPath = invocationContext.ParseResult.CommandResult.Command
            .GetPath()
            .Replace("/Commandir", string.Empty);
                            
        var commandData = _commandDataProvider.GetCommandData(commandPath);
        if(commandData == null)
            throw new Exception($"Failed to find command data data using path: {commandPath}");

        // Decide if child commands should be executed serially (the default) or in parallel.
        // This applies for all child commands - there is no way to have some children execution serially and others in parallel (yet).
        var parameterContext = new ParameterContext(invocationContext, commandData);
        
        bool parallel = false;
        object? parallelObj = parameterContext.GetParameterValue("parallel");
        if(parallelObj is not null)
        {
            parallel = Convert.ToBoolean(parallelObj);
        }

        var executables = new List<Executable>();
        GetExecutables(invocationContext, commandData, executables);

        var commandResults = new List<object?>();

        if(parallel)
        {
            var executableTasks = executables
                .Select(e => e.ExecuteAsync())
                .ToArray();
            
            await Task.WhenAll(executableTasks);
            foreach(var executableTask in executableTasks)
            {
                commandResults.Add(await executableTask);
            }
        }
        else
        {
            foreach(var executable in executables)
            {
                commandResults.Add(await executable.ExecuteAsync());
            }
        }

        return new SuccessfulCommandExecution(commandResults);
    }
}