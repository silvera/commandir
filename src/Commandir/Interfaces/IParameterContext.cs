using Stubble.Core.Builders;

namespace Commandir.Interfaces;

public interface IParameterContext
{
    Dictionary<string, object?> Parameters { get; }
    string Format(string template);
}

public sealed class ParameterContext : IParameterContext
{
    private readonly Stubble.Core.StubbleVisitorRenderer _renderer = new StubbleBuilder().Build();

    public Dictionary<string, object?> Parameters { get; }
    public string Format(string template)
    {
        return _renderer.Render(template, Parameters);   
    }

    public ParameterContext(Dictionary<string, object?> parameters)
    {
        Parameters = parameters;
    }
}