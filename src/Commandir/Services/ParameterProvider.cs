using Commandir.Interfaces;
using Microsoft.Extensions.Logging;
using Stubble.Core.Builders;

namespace Commandir.Services;

internal sealed class ParameterProvider : IParameterProvider
{
    private readonly ILogger _logger;
    private readonly Stubble.Core.StubbleVisitorRenderer _renderer;
    private readonly Dictionary<string, object?> _parameters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

    public ParameterProvider(ILoggerFactory loggerFactory)
    {
        _renderer = new StubbleBuilder().Build();
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

    public void AddOrUpdateParameters(Dictionary<string, object?> parameters)
    {
        foreach(var pair in parameters)
            _parameters[pair.Key] = pair.Value;
    }

    public object? GetParameter(string name)
    {
        _parameters.TryGetValue(name, out object? value);
        return value;
    }

    public string InterpolateParameters(string template)
    {
        return _renderer.Render(template, _parameters);
    }
}

