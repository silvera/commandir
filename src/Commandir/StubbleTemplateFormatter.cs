using Commandir.Core;
using Stubble.Core.Builders;

namespace Commandir;

internal sealed class StubbleTemplateFormatter : ITemplateFormatter
{
    private readonly Stubble.Core.StubbleVisitorRenderer _renderer;
    public StubbleTemplateFormatter()
    {
        _renderer = new StubbleBuilder().Build();
    }

    public string Format(string template, IReadOnlyDictionary<string, object?> parameters)
    {
        return _renderer.Render(template, parameters);
    }
}