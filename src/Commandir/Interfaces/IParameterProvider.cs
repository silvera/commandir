namespace Commandir.Interfaces;

public interface IParameterProvider
{
    object? GetParameter(string name);
    string InterpolateParameters(string template);
    void AddOrUpdateParameters(Dictionary<string, object?> parameters);
}