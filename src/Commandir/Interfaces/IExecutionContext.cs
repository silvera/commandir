using Microsoft.Extensions.Logging;

namespace Commandir.Interfaces;

/// <summary>
/// Encapsulates the context for an IExecutor.
/// </summary>
public interface IExecutionContext
{
    /// <summary>
    /// The LoggerFactory to construct a logger instance.
    /// </summary>
    ILoggerFactory LoggerFactory { get; }

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
    public ILoggerFactory LoggerFactory { get; }

    public CancellationToken CancellationToken { get; }

    public string Path { get; }

    public IParameterContext ParameterContext { get; } 

    public ExecutionContext(ILoggerFactory loggerFactory, CancellationToken cancellationToken, string path, IParameterContext parameterContext)
    {
        LoggerFactory = loggerFactory;
        CancellationToken = cancellationToken;
        Path = path;
        ParameterContext = parameterContext;
    }
}