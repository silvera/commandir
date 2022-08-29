using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using Microsoft.Extensions.Hosting;
using System.CommandLine.Parsing;
using Commandir.Core;
using Commandir.Actions;

namespace Commandir
{
    class Program
    {
        const string Document = @"---
            commands:
               - name: greet
                 type: shell/bash
                 run: echo ${{message}}
                 arguments:
                    - name: message
                      type: string
               - name: greeter
                 type: script/python
                 script: greet.py 
            ";

        static async Task Main(string[] args)
        {
            ConsoleAction consoleAction = new ConsoleAction((context) =>
            {
                ParameterExecutionContext? greetingContext = context.Parameters.FirstOrDefault(i => i.Name == "greeting"); 
                ParameterExecutionContext? nameContext = context.Parameters.FirstOrDefault(i => i.Name == "name");
                return $"{greetingContext?.Value} {nameContext?.Value}";
            });
            ActionHandlerRegistry registry = new ActionHandlerRegistry();
            registry.RegisterActionHandler(consoleAction);

            CommandirCommand rootCommand = new YamlCommandBuilder().Build();
            SetHandler(rootCommand, registry);

            await BuildCommandLine(rootCommand)
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

        private static void SetHandler(CommandirCommand command, ActionHandlerRegistry registry)
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
                    IActionHandler? actionHandler = registry.GetActionHandler(actionContext.Name);
                    if(actionHandler == null)
                        throw new Exception();
                    
                    ActionExecutionContext executionContext = new ActionExecutionContext(parameters);
                    await actionHandler.ExecuteAsync(executionContext);
                }
            });

            foreach(CommandirCommand subCommand in command.Subcommands)
            {
                SetHandler(subCommand, registry);
            }
        }

        private static CommandLineBuilder BuildCommandLine(Command rootCommand)
        {
            return new CommandLineBuilder(rootCommand);
        }

        // private static void Run(GreeterOptions options, IHost host)
        // {
        //     var serviceProvider = host.Services;
        //     var greeter = serviceProvider.GetRequiredService<IGreeter>();
        //     var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        //     var logger = loggerFactory.CreateLogger(typeof(Program));

        //     var name = options.Name;
        //     logger.LogInformation(GreetEvent, "Greeting was requested for: {name}", name);
        //     greeter.Greet(name);
        // }
    }
}
