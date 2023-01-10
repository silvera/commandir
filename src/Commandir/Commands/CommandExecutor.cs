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

internal sealed class ExecutableCommand
{
    private readonly IExecutor _executor;
    private readonly IExecutionContext _executionContext;

    public string Path { get; }
    public ExecutableCommand(CommandWithData command, ParameterContext parameterContext, CancellationToken cancellationToken, ILogger logger)
    {
        Path = command.GetPath();

        string? executorName = command.Data.Executor;
        _executor = executorName switch
        {
            "test" => new Test(),
            _ => new Shell()
        };
        _executionContext = new Commandir.Interfaces.ExecutionContext(logger,cancellationToken, command.GetPath(), parameterContext);
    }

    public Task<object?> ExecuteAsync()
    {
        return _executor.ExecuteAsync(_executionContext);
    }
}

internal interface IExecutionGroup
{
    string Name { get; }
    List<ExecutableCommand> Commands { get; }
    List<IExecutionGroup> Groups { get; }
    Task<List<object?>> ExecuteAsync();
    void Add(ExecutableCommand command);
    void Add(IExecutionGroup group);
}

internal abstract class ExecutionGroupBase : IExecutionGroup
{
    protected ILogger Logger { get; }
    protected ExecutionGroupBase(string name, ILogger logger)
    {
        Name = name;
        Logger = logger;
    }
    
    public string Name { get; }

    public List<ExecutableCommand> Commands { get; } = new ();
    public List<IExecutionGroup> Groups { get; } = new ();

    public virtual void Add(ExecutableCommand command) 
    {
        Logger.Debug("Adding command `{Path}` to group `{GroupName}`", command.Path, Name);
        Commands.Add(command);
    }
    public virtual void Add(IExecutionGroup group)
    {
        Logger.Debug("Adding group `{Name}` of type `{Type}` to group `{GroupName}`", group.Name, group.GetType().Name, Name);
        Groups.Add(group);
    }

    public abstract Task<List<object?>> ExecuteAsync();
}


internal sealed class SequentialExecutionGroup : ExecutionGroupBase
{
    private readonly ILogger _logger;
    public SequentialExecutionGroup(string name, ILogger logger) : base(name, logger)
    {
        _logger = logger;
    }

    private async Task<List<object?>> ExecuteAsyncCore(IExecutionGroup group)
    {
        List<object?> results = new();
        foreach(ExecutableCommand command in group.Commands)
        {
            _logger.Debug("Executing command `{CommandPath}` ({GroupName})", command.Path, Name);
            results.Add(await command.ExecuteAsync());
        }

        foreach(IExecutionGroup g in group.Groups)
        {
            _logger.Debug("Executing group `{Name}` ({GroupName})", g.Name, Name);
            List<object?> groupResults = await g.ExecuteAsync();
            results.AddRange(groupResults);
        }

        return results;
    }


    public override Task<List<object?>> ExecuteAsync()
    {
        return ExecuteAsyncCore(this);
    }
}

internal sealed class ParallelExecutionGroup : ExecutionGroupBase
{
    private readonly ILogger _logger;
    public ParallelExecutionGroup(string name, ILogger logger) : base(name, logger)
    {
        _logger = logger;
    }

    private async Task<List<object?>> ExecuteAsyncCore(IExecutionGroup group)
    {
        // Commands
        _logger.Debug("Executing commands `{Commands}` ({GroupName})", string.Join(", ", Commands.Select(c => c.Path)), Name);
        
        List<object?> results = new ();
        Task<object?>[] tasks = Commands
            .Select(e => e.ExecuteAsync())
            .ToArray();
        
        await Task.WhenAll(tasks);
        foreach(var task in tasks)
        {
            results.Add(await task);
        }

        // Groups
        foreach(IExecutionGroup g in group.Groups)
        {
            _logger.Debug("Executing group `{Group}` ({GroupName})", g.Name, Name);
            List<object?> groupResults = await g.ExecuteAsync();
            results.AddRange(groupResults);
        }

        return results;

    }

    public override Task<List<object?>> ExecuteAsync()
    {
        return ExecuteAsyncCore(this);
    }
}

/// <summary>
/// Responsibe for executing the invoked command. 
/// </summary>
internal sealed class CommandExecutor
{
    private readonly ILogger _logger;
    private readonly ILogger _loggerFactory;
    
    public CommandExecutor(ILogger logger)
    {
        _loggerFactory = logger;
        _logger = logger.ForContext<CommandExecutor>();
    }

    private void AddCommands(InvocationContext invocationContext, CommandWithData command, IExecutionGroup group)
    {
        var parameterContext = new ParameterContext(invocationContext, command);
        if(command.Subcommands.Count == 0)
        {  
            var executableCommand = new ExecutableCommand(command, parameterContext, invocationContext.GetCancellationToken(), _logger);
            group.Add(executableCommand);
        }
        else
        {
            string name = command.Name;
            bool? parallel = parameterContext.GetBooleanValue("parallel");
            bool executeCommandsInParallel = parallel ?? false;
            IExecutionGroup subGroup = executeCommandsInParallel 
                ? new ParallelExecutionGroup(name, _loggerFactory.ForContext<ParallelExecutionGroup>()) 
                : new SequentialExecutionGroup(name, _loggerFactory.ForContext<SequentialExecutionGroup>());

            group.Add(subGroup);

            foreach(CommandWithData subCommand in command.Subcommands)
            {
                AddCommands(invocationContext, subCommand, subGroup);
            }
        }
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

        IExecutionGroup rootGroup = new SequentialExecutionGroup("root", _loggerFactory.ForContext<SequentialExecutionGroup>());
        AddCommands(invocationContext, command, rootGroup);

        if(rootGroup.Commands.Count == 0 && rootGroup.Groups.Count == 0)
            throw new CommandValidationException("No executable commands were found.");
       
        List<object?> results = await rootGroup.ExecuteAsync(); 
        return new CommandExecutionResult(results);
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