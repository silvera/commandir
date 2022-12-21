using Commandir.Interfaces;
using Microsoft.Extensions.Logging;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;

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
    
    private readonly Dictionary<string, Type> _executorTypes;

    private sealed class Executable
    {
        private readonly IExecutor _executor;
        private readonly IExecutionContext _executionContext;
        public Executable(IExecutor executor, IExecutionContext executionContext)
        {
            _executor = executor;
            _executionContext = executionContext;
        }

        public Task<object?> ExecuteAsync()
        {
            return _executor.ExecuteAsync(_executionContext);
        }
    }

    public CommandExecutor(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
        _executorTypes = GetExecutorTypes();
    }

    public async Task<ICommandExecutionResult> ExecuteAsync(InvocationContext invocationContext)
    {
        // Manually surface any parse errors that should prevent command execution.
        // This is required because we're using Middlware to bypass the standard command execution pipeline.
        // This lets us handle invocations for internal commands that would normally result in a "Required command was not provided." error.
        ICommandExecutionResult? validationResult = ValidateParseResult(invocationContext);
        if(validationResult != null)
            return validationResult;

        CommandWithData? command = invocationContext.ParseResult.CommandResult.Command as CommandWithData;
        if(command == null)
            throw new Exception($"Failed to convert command to CommandWithData");
        
        // Decide if child commands should be executed serially (the default) or in parallel.
        // This applies for all child commands - there is no way to have some children execution serially and others in parallel (yet).
        var parameterContext = new ParameterContext(invocationContext, command);
        
        bool parallel = false;
        object? parallelObj = parameterContext.GetParameterValue("parallel");
        if(parallelObj is not null)
        {
            parallel = Convert.ToBoolean(parallelObj);
        }

        List<Executable> executables = new();
        GetExecutables(invocationContext, command, executables);

        List<object?> commandResults = new();
        if(parallel)
        {
            Task<object?>[] executableTasks = executables
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

    private static ICommandExecutionResult? ValidateParseResult(InvocationContext invocationContext)
    {
        IReadOnlyList<ParseError> parseErrors = invocationContext.ParseResult.Errors; 
        
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
                
                ParseError error = parseErrors.First();
             
                // If no commands were supplied, return the error.
                if(invocationContext.ParseResult.Tokens.Count == 0)
                    return new FailedCommandExecution(error.Message);
             
                // Ensure the one parse error is the expected error.
                if(error.Message != "Required command was not provided.")
                    return new FailedCommandExecution(error.Message);
            }
        }

        return null;
    }

    private void GetExecutables(InvocationContext invocationContext, CommandWithData command, List<Executable> executables)
    {   
        bool isLeafCommand = command.Subcommands.Count == 0;

        // Internal commands are not executable by default but leaf commands are.
        bool isExecutable = isLeafCommand;

        var parameterContext = new ParameterContext(invocationContext, command);
        object? executableObj = parameterContext.GetParameterValue("executable");
        if(executableObj is not null)
        {
            isExecutable = Convert.ToBoolean(executableObj);
        }

        if(isExecutable)
        {
            if(isLeafCommand)
            {
                Executable executable = GetExecutable(invocationContext, command, parameterContext);
                executables.Add(executable);
            }
            foreach(CommandWithData subCommand in command.Subcommands)
            {
                GetExecutables(invocationContext, subCommand, executables);
            }
        }
    }

    private Executable GetExecutable(InvocationContext context, CommandWithData command, ParameterContext parameterContext)
    {
        string? commandExecutor = command.Data.Executor;
        if(commandExecutor is null)
            throw new Exception($"Executor is null");

        if(!_executorTypes.TryGetValue(commandExecutor, out Type? executorType))
            throw new Exception($"Failed to find executor: {commandExecutor}");

        IExecutor? executor = Activator.CreateInstance(executorType) as IExecutor;
        if(executor == null)
            throw new Exception($"Failed to create executor: {executorType}");

        var executionContext = new Commandir.Interfaces.ExecutionContext(_loggerFactory, context.GetCancellationToken(), command.GetPath(), parameterContext);
        return new Executable(executor, executionContext);
    }
}