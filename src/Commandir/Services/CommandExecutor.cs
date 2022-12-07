using Commandir.Interfaces;
using Commandir.Yaml;
using Microsoft.Extensions.DependencyInjection;

namespace Commandir.Services;

public static class CommandExecutor
{
    public static Task<object?> ExecuteAsync(IServiceProvider services)
    {
        var dynamicCommandProvider = services.GetRequiredService<IDynamicCommandDataProvider>();
        var dynamicCommandData = dynamicCommandProvider.GetCommandData();
        if(dynamicCommandData == null)
            throw new Exception("Failed to obtain dynamic command data");

        var commandDataProvider = services.GetRequiredService<ICommandDataProvider<YamlCommandData>>();
        var commandPath = dynamicCommandData.Path;
        var commandData = commandDataProvider.GetCommandData(commandPath);
        if(commandData == null)
            throw new Exception($"Failed to find command data data using path: {commandPath}");

        var parameterProvider = services.GetRequiredService<IParameterProvider>();
        parameterProvider.AddOrUpdateParameters(commandData.Parameters);
        parameterProvider.AddOrUpdateParameters(dynamicCommandData.Parameters);

        var actionProvider = services.GetRequiredService<IActionProvider>();
        var actionType = commandData.Action!;
        var action = actionProvider.GetAction(actionType);
        if(action == null)
            throw new Exception($"Failed to find action: {actionType}");
        
        return action.ExecuteAsync(services);
    }
}