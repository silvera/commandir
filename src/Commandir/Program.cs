using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using Microsoft.Extensions.Hosting;
using System.CommandLine.Parsing;
using Commandir.Core;

namespace Commandir
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await BuildCommandLine()
            .UseHost(_ => Host.CreateDefaultBuilder(),
                host =>
                {
                    host.ConfigureServices(services =>
                    {
                    });
                })
            .UseDefaults()
            .Build()
            .InvokeAsync(args);
        }

        private static CommandLineBuilder BuildCommandLine()
        {
            ActionRegistry registry = new ActionRegistry();
            registry.RegisterActions();

            // Check for existence of file in curent directory?
            string currentDirectory = Directory.GetCurrentDirectory(); 
            string yamlFilePath = Path.Combine(currentDirectory, "Commandir.yaml");
            if(!File.Exists(yamlFilePath))
            {
                throw new InvalidOperationException($"Directory `{currentDirectory} does not contain a Commandir.yaml file");
            }
            
            var yamlFileReader = new StreamReader(yamlFilePath);
            var yamlCommandBuilder = new YamlCommandBuilder(yamlFileReader);

            RootCommand rootCommand = yamlCommandBuilder.Build();
            foreach(CommandirCommand subCommand in rootCommand.Subcommands)
            {
                SetHandler(subCommand, registry);
            }
            return new CommandLineBuilder(rootCommand);
        }

        private static void SetHandler(CommandirCommand command, ActionRegistry registry)
        {
            command.SetHandler(async invocationContext => 
            {
                List<ParameterExecutionContext> parameters = new List<ParameterExecutionContext>();
                
                // Extract Argument values
                foreach(Argument argument in command.Arguments)
                {
                    object? value = invocationContext.ParseResult.GetValueForArgument(argument);
                    ParameterExecutionContext parameterContext = new ParameterExecutionContext(argument.Name, value);
                    parameters.Add(parameterContext);
                }
                // Extract Option values
                foreach(Option option in command.Options)
                {
                    object? value = invocationContext.ParseResult.GetValueForOption(option);
                    ParameterExecutionContext parameterContext = new ParameterExecutionContext(option.Name, value);
                    parameters.Add(parameterContext);
                }
                
                // Create ActionExecutionContext
                foreach(ActionContext actionContext in command.Actions)
                {
                    ActionExecutionContext executionContext = new ActionExecutionContext(parameters);

                    IAction? action = registry.GetAction(actionContext.Name);
                    if(action == null)
                        throw new Exception();
                    
                    await action.ExecuteAsync(executionContext);
                }
            });

            foreach(CommandirCommand subCommand in command.Subcommands)
            {
                SetHandler(subCommand, registry);
            }
        }
    }
}
