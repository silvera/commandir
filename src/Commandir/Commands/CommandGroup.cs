using Serilog;

namespace Commandir.Commands;

internal interface ICommandGroup
{
    string Name { get; }
    List<ICommand> Commands { get; }
    List<ICommandGroup> Groups { get; }
    void Add(ICommand command);
    void Add(ICommandGroup group);
    Task<List<object?>> ExecuteAsync();
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