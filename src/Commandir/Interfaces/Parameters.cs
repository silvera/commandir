namespace Commandir.Interfaces;

public interface IParameterProvider
{
    object? GetParameter(string name);
    Dictionary<string, object?> GetParameters();

    void AddOrUpdateParameter(string name, object? value);
}