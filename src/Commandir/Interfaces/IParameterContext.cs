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

    /// <summary>
    /// Returns the value of the requested parameter as a boolean or null if not found.
    /// </summary>
    bool? GetBooleanValue(string parameterName);
}