using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine.Parsing;

namespace Commandir
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await BuildCommandLine()
            .UseHost(_ => Host.CreateDefaultBuilder(args),
                host =>
                { 
                    host.ConfigureServices(services => 
                    {
                        services.AddActions();
                    });
                })
            .UseDefaults()
            .Build()
            .InvokeAsync(args);
        }

        private static async Task HandleAsync(IHost host)
        {
            ActionTypeRegistry actionTypeRegistry = host.Services.GetRequiredService<ActionTypeRegistry>();
            
            IActionContextProvider provider = host.Services.GetRequiredService<IActionContextProvider>();
            foreach(ActionData actionData in provider.GetActions())
            {
                Type? actionType = actionTypeRegistry.GetType(actionData.Name);
                if(actionType == null)
                    throw new Exception($"Failed to find ActionType for type `{actionData.Name}`");

                Action? action = host.Services.GetRequiredService(actionType) as Action;
                if(action == null)
                    throw new Exception($"Failed to find Action for type `{actionType}`");

                await action.ExecuteAsync(provider.GetCancellationToken());
            }
        }

        private static CommandLineBuilder BuildCommandLine()
        {
            // Check for existence of file in curent directory?
            string currentDirectory = Directory.GetCurrentDirectory(); 
            string yamlFilePath = Path.Combine(currentDirectory, "Commandir.yaml");
            if(!File.Exists(yamlFilePath))
            {
                throw new InvalidOperationException($"Directory `{currentDirectory} does not contain a Commandir.yaml file");
            }
            
            var yamlFileReader = new StreamReader(yamlFilePath);
            var yamlCommandBuilder = new YamlCommandBuilder(yamlFileReader);

            Command rootCommand = yamlCommandBuilder.Build(HandleAsync);
            return new CommandLineBuilder(rootCommand);
        }
    }
}
