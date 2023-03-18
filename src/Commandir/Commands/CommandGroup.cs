using Commandir.Interfaces;
using Commandir.Executors;
using Serilog;

namespace Commandir.Commands;

internal interface IExecutable
{
    string Name { get; }
    Task<object?> ExecuteAsync();
}

internal sealed class Executable : IExecutable
{
    private readonly IExecutor _executor;
    private readonly IExecutionContext _executionContext;

    public string Name => Command.GetPath();

    public CommandWithData Command { get; }
    public Executable(CommandWithData command, ParameterContext parameterContext, CancellationToken cancellationToken, ILogger logger)
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

internal interface ICommandGroup
{
    string Name { get; }
    List<IExecutable> Commands { get; }
    List<ICommandGroup> Groups { get; }
    void Add(IExecutable command);
    void Add(ICommandGroup group);
    Task<List<object?>> ExecuteAsync();
}

internal abstract class CommandGroupBase : ICommandGroup
{
    protected ILogger Logger { get; }
    protected CommandGroupBase(string name, ILogger logger)
    {
        Name = name;
        Logger = logger;
    }
    
    public string Name { get; }

    public List<IExecutable> Commands { get; } = new ();
    public List<ICommandGroup> Groups { get; } = new ();

    public virtual void Add(IExecutable executable) 
    {
        Logger.Debug("Adding command `{Name}` to group `{GroupName}`", executable.Name, Name);
        Commands.Add(executable);
    }
    public virtual void Add(ICommandGroup group)
    {
        Logger.Debug("Adding group `{Name}` of type `{Type}` to group `{GroupName}` ", group.Name, group.GetType().Name, Name);
        Groups.Add(group);
    }

    public abstract Task<List<object?>> ExecuteAsync();
}

internal sealed class SerialCommandGroup : CommandGroupBase
{
    public SerialCommandGroup(string name, ILogger logger) : base(name, logger.ForContext<SequentialCommandGroup>())
    {
    }

    private async Task<List<object?>> ExecuteAsyncCore(ICommandGroup group)
    {
        List<object?> results = new();
        foreach(IExecutable executable in group.Commands)
        {
            Logger.Debug("Executing command `{Path}` on group `{GroupName}`", executable.Name, Name);
            results.Add(await executable.ExecuteAsync());
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

internal sealed class ParallelCommandGroup : CommandGroupBase
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