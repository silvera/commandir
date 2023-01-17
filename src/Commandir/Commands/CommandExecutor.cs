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

internal interface ICommand
{
    string Name { get; }
    Task<object?> ExecuteAsync();
}

internal interface ICommandGroup
{
    string Name { get; }
    List<ICommand> Commands { get; }
    List<ICommandGroup> Groups { get; }
    void Add(ICommand command);
    void Add(ICommandGroup group);
    Task<List<object?>> ExecuteAsync();
}

internal sealed class ExecutableCommand : ICommand
{
    private readonly IExecutor _executor;
    private readonly IExecutionContext _executionContext;

    public string Name => Command.GetPath();
    public string Path => Command.GetPath();
    public CommandWithData Command { get; }
    public ExecutableCommand(CommandWithData command, ParameterContext parameterContext, CancellationToken cancellationToken, ILogger logger)
    {
        Command = command;

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

internal abstract class CommandGroup : ICommandGroup
{
    protected ILogger Logger { get; }
    protected CommandGroup(string name, ILogger logger)
    {
        Name = name;
        Logger = logger;
    }
    
    public string Name { get; }

    public List<ICommand> Commands { get; } = new ();
    public List<ICommandGroup> Groups { get; } = new ();

    public virtual void Add(ICommand command) 
    {
        Logger.Debug("Adding command `{Name}` to group `{GroupName}`", command.Name, Name);
        Commands.Add(command);
    }
    public virtual void Add(ICommandGroup group)
    {
        Logger.Debug("Adding group `{Name}` of type `{Type}` to group `{GroupName}` ", group.Name, group.GetType().Name, Name);
        Groups.Add(group);
    }

    public abstract Task<List<object?>> ExecuteAsync();
}


internal sealed class SequentialCommandGroup : CommandGroup
{
    public SequentialCommandGroup(string name, ILogger logger) : base(name, logger.ForContext<SequentialCommandGroup>())
    {
    }

    private async Task<List<object?>> ExecuteAsyncCore(ICommandGroup group)
    {
        List<object?> results = new();
        foreach(ExecutableCommand command in group.Commands)
        {
            Logger.Debug("Executing command `{Path}` on group `{GroupName}`", command.Path, Name);
            results.Add(await command.ExecuteAsync());
        }

        foreach(ICommandGroup g in group.Groups)
        {
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

internal sealed class ParallelCommandGroup : CommandGroup
{
    public ParallelCommandGroup(string name, ILogger logger) : base(name, logger.ForContext<ParallelCommandGroup>())
    {
    }

    private async Task<List<object?>> ExecuteAsyncCore(ICommandGroup group)
    {
        List<object?> results = new ();
        
        if(Commands.Count > 0)
        {
            // Commands
            Logger.Debug("Executing commands `{Commands}` on group `{GroupName}`", string.Join(", ", Commands.Select(c => c.Name)), Name);
            
            Task<object?>[] tasks = Commands
                .Select(e => e.ExecuteAsync())
                .ToArray();
            
            await Task.WhenAll(tasks);
            foreach(var task in tasks)
            {
                results.Add(await task);
            }
        }

        if(Groups.Count > 0)
        {
            Logger.Debug("Executing groups `{Groups}` on group `{GroupName}`", string.Join(", ", Groups.Select(c => c.Name)), Name);
            Task<List<object?>>[] tasks = Groups
                .Select(e => e.ExecuteAsync())
                .ToArray();

            await Task.WhenAll(tasks);
            foreach(var task in tasks)
            {
                results.AddRange(await task);
            }
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

    private ICommandGroup GetCommandGroup(CommandWithData command, ParameterContext parameterContext)
    {
        string name = command.Name;
        bool? parallel = parameterContext.GetBooleanValue("parallel");
        bool executeCommandsInParallel = parallel ?? false;
        return executeCommandsInParallel 
            ? new ParallelCommandGroup(name, _loggerFactory) 
            : new SequentialCommandGroup(name, _loggerFactory);

    }
    private void AddCommands(InvocationContext invocationContext, CommandWithData command, ICommandGroup group, bool isRootGroup)
    {
        var parameterContext = new ParameterContext(invocationContext, command);

        if(command.Subcommands.Count == 0)
        {  
            var executableCommand = new ExecutableCommand(command, parameterContext, invocationContext.GetCancellationToken(), _logger);
            group.Add(executableCommand);
        }
        else
        {
            ICommandGroup? subGroup = null;
            if(isRootGroup)
            {
                subGroup = group;
            }
            else
            {
                subGroup = GetCommandGroup(command, parameterContext);
                group.Add(subGroup);
            }
            
            foreach(CommandWithData subCommand in command.Subcommands)
            {
                AddCommands(invocationContext, subCommand, subGroup, false);
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

        _logger.Debug("Executing command `{CommandPath}`", command.GetPath());

        ICommandGroup commandGroup = GetCommandGroup(command, new ParameterContext(invocationContext, command));
        AddCommands(invocationContext, command, commandGroup, isRootGroup: true);

        if(commandGroup.Commands.Count == 0 && commandGroup.Groups.Count == 0)
            throw new CommandValidationException("No executable commands were found.");
       
        List<object?> results = await commandGroup.ExecuteAsync(); 
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
}