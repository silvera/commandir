namespace Commandir.Core;

public sealed class CommandContext
{
    public IServiceProvider Services { get; }
    public CancellationToken CancellationToken { get; }
    public IReadOnlyDictionary<string, object?> Parameters { get; }

    public CommandContext(IServiceProvider services, CancellationToken cancellationToken, IReadOnlyDictionary<string, object?> parameters)
    {
        Services = services;
        Parameters = parameters;
        CancellationToken = cancellationToken;
    }
}