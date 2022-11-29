using Commandir.Commands;
using Commandir.Interfaces;
using Commandir.Services;
using Commandir.Yaml;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;

namespace Commandir
{
    public class Program
    {
        
        public static async Task<int> Main(string[] args)
        {
            Serilog.Events.LogEventLevel commandirLogLevel = Serilog.Events.LogEventLevel.Information;
            if(args.Length > 0)
            {
                if(string.Equals(args[0], "--verbose", StringComparison.OrdinalIgnoreCase))
                    commandirLogLevel = Serilog.Events.LogEventLevel.Verbose;
            }

            var logger = new LoggerConfiguration()
                .MinimumLevel.Override("Commandir", commandirLogLevel)
                .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Error)
                .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
                .WriteTo.Console(outputTemplate: "{SourceContext}: {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            Log.Logger = logger;
            ILoggerFactory loggerFactory = new LoggerFactory().AddSerilog(logger);

            Commandir.Core.CommandResult? commandResult = null;

            try
            {
                await BuildCommandLine(loggerFactory, i => commandResult = i)
                .UseHost(host => {
                    host.UseSerilog(logger);
                    host.ConfigureServices(services =>
                    {
                        services.AddSingleton<IParameterProvider, ParameterProvider>();
                        services.AddSingleton<IActionHandlerProvider, ActionHandlerProvider>();
                        services.AddSingleton<ITemplateFormatter2, StubbleTemplateFormatter2>();
                    });
                })
                .UseDefaults()
                .Build()
                .InvokeAsync(args);

                return commandResult?.ReturnCode ?? 1;
            }
            catch(Exception e)
            {
                loggerFactory
                    .CreateLogger<Program>()
                    .LogCritical("{ExceptionType}: {Message}", e.GetType().Name,  e.Message);
                
                return 1;
            }
        }

        private static CommandLineBuilder BuildCommandLine(ILoggerFactory loggerFactory, Action<Commandir.Core.CommandResult> commandResultHandler)
        {       
            string yamlFile = Path.Combine(Directory.GetCurrentDirectory(), "Commandir.yaml");
            string yaml = File.ReadAllText(yamlFile);
            
            var rootCommand = YamlCommandParser.Parse(yaml);
            rootCommand.SetHandlers(HandleInvocationAsync, e => Console.WriteLine(e.Message));
            return new CommandLineBuilder(rootCommand);
        }

        private static async Task HandleInvocationAsync(IServiceProvider services)
        {
            var invocationContext = services.GetRequiredService<InvocationContext>();
            var parseResult = invocationContext.ParseResult;
            var command = (CommandirCommand)parseResult.CommandResult.Command;
            
            var actionHandlerProvider = services.GetRequiredService<IActionHandlerProvider>();
            var actionName = command.Action!;
            var action = actionHandlerProvider.GetAction(actionName);
            if(action == null)
                throw new ArgumentException($"Failed to find action: {actionName}");
          
            var parameterProvider = services.GetRequiredService<IParameterProvider>();
            foreach(var pair in command.Parameters)
            {
                if(pair.Value != null)
                    parameterProvider.AddOrUpdateParameter(pair.Key, pair.Value);
            }

            // Add command line parameters after command parameters so they can override the parameters.
            foreach(Argument argument in command.Arguments)
            {
                object? value = parseResult.GetValueForArgument(argument);
                if(value != null)
                {
                    parameterProvider.AddOrUpdateParameter(argument.Name, value);
                }
            }

            foreach(Option option in command.Options)
            {
                object? value = parseResult.GetValueForOption(option);
                if(value != null)
                {
                    parameterProvider.AddOrUpdateParameter(option.Name, value);
                } 
            }

            var request = new ActionRequest(services, default(CancellationToken));
            var response = await action.HandleAsync(request);
            Console.WriteLine($"Received response: {response}");
        }
    }
}