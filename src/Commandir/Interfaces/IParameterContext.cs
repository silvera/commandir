namespace Commandir.Interfaces;

/// <summary>
/// Encapsulates the parameters available to an IExecutor.
/// </summary>
public interface IParameterContext
{
    /// <summary>
    /// Returns the template with any parameters interpolated.
    /// </summary>
    string FormatParameters(string template);
    
    /// <summary>
    /// Returns the value of the requested parameter or null.
    /// </summary>
    object? GetParameterValue(string parameterName);
}