using Microsoft.Extensions.Logging;

namespace Commandir.Interfaces;

public interface IParameterContext
{
    Dictionary<string, object?> Parameters { get; }
    string Format(string template);
}

public interface IExecutionContext
{
    ILoggerFactory LoggerFactory { get; }
    CancellationToken CancellationToken { get; }
    string Path {get; }
    IParameterContext ParameterContext { get; }
}

public interface IExecutor
{
    Task<object?> ExecuteAsync(IExecutionContext context);
}