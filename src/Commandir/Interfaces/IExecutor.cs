namespace Commandir.Interfaces;

/// <summary>
/// Encapsulates the logic invoked when a command is executed. 
/// Implementations may execute a command, query a database, issue a HTTP request etc.
/// </summary>
public interface IExecutor
{
    /// <summary>
    /// Perform the logic using the supplied IExecutionContext.
    /// </summary>
    Task<object?> ExecuteAsync(IExecutionContext context);
}
