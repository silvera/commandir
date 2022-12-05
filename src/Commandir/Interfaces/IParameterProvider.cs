namespace Commandir.Interfaces;

public interface IParameterProvider
{
    
    Dictionary<string, object?> GetParameters();

    void AddOrUpdateParameter(string name, object? value);
    
    
    object? GetParameter(string name);
    string InterpolateParameters(string template);
    void AddOrUpdateParameters(Dictionary<string, object?> parameters);
}