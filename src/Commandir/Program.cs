using System.CommandLine;
using System.CommandLine.Binding;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.CommandLine.Parsing;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.IO;
using Commandir.Core;

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
            StringReader reader = new StringReader(Document);
            //YamlCommandSource commandSource = new YamlCommandSource(reader);

            // What if we created the commands first and then wired up the handlers afterwards?
            // Then we could use 

            //IReadOnlyCollection<Command> commands = commandSource.GetCommands();
            // var commandDefinitionProvider = new TestCommandDefinitionProvider();
            // CommandDefinition commandDefinitions = commandDefinitionProvider.GetDefinitions();
            // CommandBuilder commandBuilder = new CommandBuilder();
            // Command command = commandBuilder.Build(commandDefinitions);
            CommandHandler commandHandler = new CommandHandler();
            Command rootCommand = new YamlCommandBuilder(commandHandler).Build();
            await BuildCommandLine(rootCommand)
            .UseHost(_ => Host.CreateDefaultBuilder(),
                host =>
                {
                    host.ConfigureServices(services =>
                    {

                        //services.AddSingleton<IGreeter, Greeter>();
                    });
                })
            .UseDefaults()
            .Build()
            .InvokeAsync(args);
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
