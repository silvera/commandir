using Commandir.Interfaces;
using Microsoft.Extensions.Logging;

namespace Commandir.Services;


internal sealed class ParameterProvider : IParameterProvider
{
    private readonly ILogger _logger;
    private readonly Dictionary<string, object?> _parameters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

    public ParameterProvider(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<ParameterProvider>();
    }

    public void AddOrUpdateParameter(string name, object? value)
    {
        _parameters[name] = value;
    }

    public Dictionary<string, object?> GetParameters()
    {
        return _parameters;
    }

    public object? GetParameter(string name)
    {
        _parameters.TryGetValue(name, out object? value);
        return value;
    }
}

