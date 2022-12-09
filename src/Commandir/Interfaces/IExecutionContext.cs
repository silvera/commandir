using Microsoft.Extensions.Logging;

namespace Commandir.Interfaces;

public interface IExecutionContext
{
    ILoggerFactory LoggerFactory { get; }
    CancellationToken CancellationToken { get; }
    string Path {get; }
    IParameterContext ParameterContext { get; }
}

public sealed class ExecutionContext : IExecutionContext
{
    public ILoggerFactory LoggerFactory { get; }

    public CancellationToken CancellationToken { get; }

    public string Path { get; }

    public IParameterContext ParameterContext { get; } 

    public ExecutionContext(ILoggerFactory loggerFactory, CancellationToken cancellationToken, string path, Dictionary<string, object?> parameters)
    {
        LoggerFactory = loggerFactory;
        CancellationToken = cancellationToken;
        Path = path;
        ParameterContext = new ParameterContext(parameters);
    }
}