using Microsoft.Extensions.Logging;

namespace Commandir.Interfaces;

public interface IExecutor
{
    Task<object?> ExecuteAsync(IExecutionContext context);
}