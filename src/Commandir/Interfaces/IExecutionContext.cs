using Serilog;

namespace Commandir.Interfaces;

/// <summary>
/// Encapsulates the context for an IExecutor.
/// </summary>
public interface IExecutionContext
{
    /// <summary>
    /// The Logger to construct a logger instance.
    /// </summary>
    ILogger Logger  { get; }

    /// <summary>
    /// A CancellationToken to cancel the executor's execution. 
    /// </summary>
    CancellationToken CancellationToken { get; }

    /// <summary>
    /// The command path.
    /// Consider a command 'hello' with subcommand 'world'.
    /// The path for 'hello' is '/Commandir/hello/'.
    /// The path for 'world' is '/Commandir/hello/world'.
    /// </summary>
    string Path {get; }


    /// <summary>
    /// The PaameterContext for this ExecutionContext.
    /// </summary>
    IParameterContext ParameterContext { get; }
}

internal sealed class ExecutionContext : IExecutionContext
{
    public ILogger Logger { get; }

    public CancellationToken CancellationToken { get; }

    public string Path { get; }

    public IParameterContext ParameterContext { get; } 

    public ExecutionContext(ILogger logger, CancellationToken cancellationToken, string path, IParameterContext parameterContext)
    {
        Logger = logger;
        CancellationToken = cancellationToken;
        Path = path;
        ParameterContext = parameterContext;
    }
}