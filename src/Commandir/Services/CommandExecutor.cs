using Commandir.Interfaces;
using Commandir.Yaml;
using Microsoft.Extensions.DependencyInjection;

namespace Commandir.Services;

public static class CommandExecutor
{
    private static List<YamlCommandData> GetParentCommands(YamlCommandData commandData)
    {
        var components = new List<YamlCommandData>();
        var current = commandData.Parent;
        while(current != null)
        {
            components.Add(current);
            current = current.Parent;
        }

        components.Reverse();
        return components;
    }

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

        // Add static (configured) parameters from all parent commands.
        var parentCommands = GetParentCommands(commandData);
        foreach(var parentCommand in parentCommands)
        {
            parameterProvider.AddOrUpdateParameters(parentCommand.Parameters);
        }

        // Add static parameters from this command.
        parameterProvider.AddOrUpdateParameters(commandData.Parameters);

        // Add dynamic parameters from this command.
        parameterProvider.AddOrUpdateParameters(dynamicCommandData.Parameters);

        var actionProvider = services.GetRequiredService<IActionProvider>();
        var actionType = commandData.Action!;
        var action = actionProvider.GetAction(actionType);
        if(action == null)
            throw new Exception($"Failed to find action: {actionType}");
        
        return action.ExecuteAsync(services);
    }
}