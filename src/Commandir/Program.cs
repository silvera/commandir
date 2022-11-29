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
        private static Microsoft.Extensions.Logging.ILogger? s_logger;

        public static async Task Main(string[] args)
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
                .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Error)
                .WriteTo.Console(outputTemplate: "{SourceContext}: {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            ILoggerFactory loggerFactory = new LoggerFactory().AddSerilog(logger);
            s_logger = loggerFactory.CreateLogger<Program>();

            try
            {
                await BuildCommandLine()
                .UseHost(host => {
                    host.UseSerilog(logger);
                    host.ConfigureServices(services =>
                    {
                        services.AddCommandirServices();
                    });
                })
                .UseDefaults()
                .Build()
                .InvokeAsync(args);
            }
            catch(Exception e)
            {
                s_logger.LogCritical("{ExceptionType}: {Message}", e.GetType().Name,  e.Message);
            }
        }

        private static CommandLineBuilder BuildCommandLine()
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

            var request = new ActionRequest(services, invocationContext.GetCancellationToken());
            var response = await action.HandleAsync(request);
            if(response.Value is int exitCode)
            {
                s_logger?.LogInformation("Response: ExitCode: {ExitCode}", exitCode);
            }
        }
    }
}