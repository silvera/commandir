namespace Commandir.Core;

public interface ITemplateFormatter
{
    string Format(string template, IReadOnlyDictionary<string, object?> parameters);
}