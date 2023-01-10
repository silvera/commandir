using Commandir.Interfaces;
using Commandir.Executors;
using Serilog;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;

namespace Commandir.Commands;

public class CommandValidationException : Exception
{
    public CommandValidationException(string message)
        : base(message)
    {
    }
}

public sealed class CommandExecutionResult
{
    /// <summary>
    /// The results of the execution of a command.
    /// May contain the results of multiple subcommands. 
    /// </summary>
    public IEnumerable<object?> Results { get; }
    public CommandExecutionResult(IEnumerable<object?> results)
    {
        Results = results;
    }
}

/// <summary>
/// Responsibe for executing the invoked command. 
/// </summary>
internal sealed class CommandExecutor
{
    private readonly ILogger _logger;
    
    public CommandExecutor(ILogger logger)
    {
        _logger = logger.ForContext<CommandExecutor>();
    }

    public async Task<CommandExecutionResult> ExecuteAsync(InvocationContext invocationContext)
    {
        // Manually surface any parse errors that should prevent command execution.
        // This is required because we're using Middlware to bypass the standard command execution pipeline.
        // This lets us handle invocations for internal commands that would normally result in a "Required command was not provided." error.
        ValidateCommandInvocation(invocationContext);

        CommandWithData? command = invocationContext.ParseResult.CommandResult.Command as CommandWithData;
        if(command == null)
            throw new Exception($"Failed to convert command to CommandWithData");

        _logger.Debug("Invoking command: {CommandPath}", command.GetPath());

        List<Executable> executables = new();
        GetExecutableCommands(invocationContext, command, executables);

        if(executables.Count == 0)
            throw new CommandValidationException("No executable commands were found.");
       
        var parameterContext = new ParameterContext(invocationContext, command);

        // Decide if child commands should be executed serially (the default) or in parallel.
        // This applies for all child commands - there is no way to have some children execution serially and others in parallel (yet).
        bool? parallel = parameterContext.GetBooleanValue("parallel");
        bool executeCommandsInParallel = parallel ?? false;

        List<object?> commandResults = new();
        if(executeCommandsInParallel)
        {
            // Execute tasks in parallel and wait for them to finish.
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
            // Execute tasks serially.
            foreach(var executable in executables)
            {
                commandResults.Add(await executable.ExecuteAsync());
            }
        }

        return new CommandExecutionResult(commandResults);
    }

    private static void ValidateCommandInvocation(InvocationContext invocationContext)
    {
        IReadOnlyList<ParseError> parseErrors = invocationContext.ParseResult.Errors; 
        
        // Check for unexpected parse errors
        if(parseErrors.Count > 0)
        {
            if(parseErrors.Count > 1)
            {
                // There is more than one parse error; return the last error.
                throw new CommandValidationException(parseErrors.Last().Message);
            }
            else
            {
                ParseError error = parseErrors.First();
             
                // If no commands were supplied, return the error.
                if(invocationContext.ParseResult.Tokens.Count == 0)
                    throw new CommandValidationException(error.Message);
             
                // Ensure the one parse error is the expected error.
                if(error.Message != "Required command was not provided.")
                    throw new CommandValidationException(error.Message);
            }
        }
    }

    // Encapsulates an executor with its execution context (a closure).
    private sealed class Executable
    {
        private readonly IExecutor _executor;
        private readonly IExecutionContext _executionContext;
        private readonly ILogger _logger;
        public Executable(IExecutor executor, IExecutionContext executionContext, ILogger logger)
        {
            _executor = executor;
            _executionContext = executionContext;
            _logger = logger;
        }

        public Task<object?> ExecuteAsync()
        {
            _logger.Debug("Executing command: {CommandPath}", _executionContext.Path);
            return _executor.ExecuteAsync(_executionContext);
        }
    }    

    private void GetExecutableCommands(InvocationContext invocationContext, CommandWithData command, List<Executable> executableCommands)
    {
        // All commands are executable by default.
        bool isExecutableCommand = true;

        var parameterContext = new ParameterContext(invocationContext, command);
        bool? isExecutable = parameterContext.GetBooleanValue("executable");
        if(isExecutable.HasValue)
        {
            isExecutableCommand = isExecutable.Value;
        }

        if(isExecutableCommand)
        {
            if(command.Subcommands.Count == 0)
            {
                // Leaf commands are actually executable.
                Executable executableCommand = GetExecutable(invocationContext, command, parameterContext);
                executableCommands.Add(executableCommand);
            }

            foreach(CommandWithData subCommand in command.Subcommands)
            {
                GetExecutableCommands(invocationContext, subCommand, executableCommands);
            }
        }
    }

    private Executable GetExecutable(InvocationContext context, CommandWithData command, ParameterContext parameterContext)
    {
        string? executorName = command.Data.Executor;
        IExecutor executor = executorName switch
        {
            "test" => new Test(),
            _ => new Shell()
        };
        var executionContext = new Commandir.Interfaces.ExecutionContext(_logger, context.GetCancellationToken(), command.GetPath(), parameterContext);
        return new Executable(executor, executionContext, _logger);
    }
}