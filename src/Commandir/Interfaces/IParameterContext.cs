namespace Commandir.Interfaces;

public interface IParameterContext
{
    string FormatParameters(string template);
    object? GetParameterValue(string parameterName);
}