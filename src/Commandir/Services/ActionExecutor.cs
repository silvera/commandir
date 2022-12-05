using Commandir.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Commandir.Services;

public class ActionExecutor : IActionExecutor
{
    private readonly IServiceProvider _services;
    public ActionExecutor(IServiceProvider services)
    {
        _services = services;
    }

    public async Task<ActionResponse> ExecuteAsync(ActionExecutionData executionData)
    {
        var actionHandlerProvider = _services.GetRequiredService<IActionHandlerProvider>();
        var action = actionHandlerProvider.GetAction(executionData.Action);
        if(action == null)
            throw new ArgumentException($"Failed to find action: {executionData.Action}");

        var parameterProvider = _services.GetRequiredService<IParameterProvider>();
        foreach(var pair in executionData.Parameters)
            parameterProvider.AddOrUpdateParameter(pair.Key, pair.Value);

        var request = new ActionRequest(_services, executionData.CancellationToken);
        return await action.HandleAsync(request);
    }
}