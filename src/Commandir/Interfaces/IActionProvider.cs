using Microsoft.Extensions.DependencyInjection;

namespace Commandir.Interfaces;

public interface IAction
{
    Task<object?> ExecuteAsync(IServiceProvider services);
}

public interface IActionProvider
{
    IAction? GetAction(string actionName);
}
