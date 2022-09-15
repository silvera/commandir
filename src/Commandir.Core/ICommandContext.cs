namespace Commandir.Core;

public interface ICommandContext
{
    IServiceProvider Services { get; }
    IReadOnlyDictionary<string, object?> Parameters { get; }
}