using Commandir.Core;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Commandir
{
    public class CommandProvider
    {
        private readonly ILogger _logger;
        private readonly Dictionary<string, Type> _commandTypes = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

        public CommandProvider(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<CommandProvider>();
        }

        public void AddCommands(Assembly assembly)
        {
            foreach (Type type in assembly.GetTypes())
            {
                if (typeof(ICommand).IsAssignableFrom(type))
                {
                    string typeName = type.FullName!;

                    _commandTypes.Add(typeName, type);
                    _logger.LogInformation("Adding Command: {Type}", typeName);
                }
            }
        }

        public ICommand? GetCommand(string fullName)
        {
            if(!_commandTypes.TryGetValue(fullName, out Type? commandType))
                return null;

            return Activator.CreateInstance(commandType) as ICommand;
        }
    }
}