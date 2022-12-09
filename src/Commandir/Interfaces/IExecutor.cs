using Microsoft.Extensions.Logging;

namespace Commandir.Interfaces;

// public interface IParameterFormatter
// {
//     string Format(string template, Dictionary<string, object?> parameters);
// }

public interface IParameterContext
{
    Dictionary<string, object?> Parameters { get; }
    string Format(string template);
}

public interface IExecutionContext
{
    ILoggerFactory LoggerFactory { get; }
    CancellationToken CancellationToken { get; }
    // Dictionary<string, object?> Parameters { get; }
    // IParameterFormatter ParameterFormatter { get; }
    string Path {get; }
    IParameterContext ParameterContext { get; }
}


public interface IExecutor
{
    Task<object?> ExecuteAsync(IExecutionContext context);
}