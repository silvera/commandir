using Commandir.Interfaces;
using Stubble.Core.Builders;

namespace Commandir.Services;

internal sealed class StubbleTemplateFormatter2 : ITemplateFormatter2
{
    private readonly Stubble.Core.StubbleVisitorRenderer _renderer;
    public StubbleTemplateFormatter2()
    {
        _renderer = new StubbleBuilder().Build();
    }

    public string Format(string template, IReadOnlyDictionary<string, object?> parameters)
    {
        return _renderer.Render(template, parameters);
    }
}