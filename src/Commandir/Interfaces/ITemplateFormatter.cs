namespace Commandir.Interfaces;

public interface ITemplateFormatter2
{
    string Format(string template, IReadOnlyDictionary<string, object?> parameters);
}