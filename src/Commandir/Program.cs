using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Reflection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

using Commandir.Core;

namespace Commandir
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                await BuildCommandLine()
                .UseHost(_ => Host.CreateDefaultBuilder(args),
                    host =>
                    { 
                        host.ConfigureServices(services => 
                        {
                            services.RegisterCommands();
                        });
                    })
                .UseDefaults()
                .Build()
                .InvokeAsync(args);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private static async Task HandleAsync(IHost host)
        {            
            InvocationContext invocationContext = host.Services.GetRequiredService<InvocationContext>();
            CommandLineCommand? command = invocationContext.ParseResult.CommandResult.Command as CommandLineCommand;
            if(command == null)
                throw new Exception("Failed to obtain command");

            string? commandType  = command.CommandData.Type;
            if(commandType == null)
                throw new Exception("Command Type was null");

            ICommandRegistry commandRegistry = host.Services.GetRequiredService<ICommandRegistry>();
            if(commandRegistry == null)
                throw new Exception("Command Registry was null");

            ICommand? commandImpl = commandRegistry.GetCommand(commandType);
            if(commandImpl == null)
                throw new Exception("Failed to find command impl");

            await commandImpl.ExecuteAsync(); 
        }

        private static CommandLineBuilder BuildCommandLine()
        {
            string currentDirectory = Directory.GetCurrentDirectory(); 
            string yamlFilePath = Path.Combine(currentDirectory, "Commandir.yaml");
            if(!File.Exists(yamlFilePath))
                throw new FileNotFoundException($"No Commandir.yaml file found in {currentDirectory}", "Commandir.yaml");

            string yaml = File.ReadAllText(yamlFilePath);
            Core.CommandData rootData  = new YamlCommandDataBuilder2(yaml).Build();
            CommandLineCommand rootCommand = new CommandBuilder(rootData, HandleAsync).Build(); 
            return new CommandLineBuilder(rootCommand);
        }
    }

    public static class ServiceExtensions
    {
        public static IServiceCollection RegisterCommands(this IServiceCollection services)
        {
            CommandRegistry registry = new CommandRegistry();
            registry.RegisterCommands(typeof(Program).Assembly);
            services.AddSingleton(typeof(ICommandRegistry), sp => registry);
            return services;
        }
    }

    public interface ICommandRegistry
    {
        void RegisterCommands(Assembly assembly);
        ICommand? GetCommand(string fullName);
    }

    public class CommandRegistry : ICommandRegistry
    {
        private readonly Dictionary<string, ICommand> _commands = new Dictionary<string, ICommand>(StringComparer.OrdinalIgnoreCase);
        
        public void RegisterCommands(Assembly assembly)
        {
            List<ICommand> commands = CreateCommands(assembly);
            foreach(ICommand command in commands)
            {
                string? fullName = command.GetType().FullName;
                if(fullName != null)
                    _commands[fullName] = command;
            }
        }

        private static List<ICommand> CreateCommands(Assembly assembly)
        {
            List<ICommand> commands = new List<ICommand>();
            foreach (Type type in assembly.GetTypes())
            {
                if (typeof(ICommand).IsAssignableFrom(type))
                {
                    ICommand? command = Activator.CreateInstance(type) as ICommand;
                    if (command != null)
                    {
                        commands.Add(command);
                    }
                }
            }
            return commands;
        }

        public ICommand? GetCommand(string fullName)
        {
            return _commands.TryGetValue(fullName, out ICommand? command) ? command : null;
        }
    }
}
