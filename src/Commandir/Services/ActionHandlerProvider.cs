using Commandir.Interfaces;
using Microsoft.Extensions.Logging;

namespace Commandir.Services;

public sealed class ActionHandlerProvider : IActionHandlerProvider
{
    private readonly ILogger _logger;
    private readonly Dictionary<string, Type> _types = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

    public ActionHandlerProvider(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<ActionHandlerProvider>();
        foreach (Type type in typeof(Program).Assembly.GetTypes())
        {
            if (typeof(IActionHandler).IsAssignableFrom(type))
            {
                string typeName = type.FullName!;

                _types.Add(typeName, type);
                _logger.LogInformation("Adding Action: {Type}", typeName);
            }
        }
    }

    public IActionHandler? GetAction(string actionName)
    {
        if(!_types.TryGetValue(actionName, out Type? type))
            return null;

        return Activator.CreateInstance(type) as IActionHandler;
    }
}