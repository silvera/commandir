﻿using Commandir.Commands;
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
        private static YamlCommandDataProvider s_commandDataProvider;

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

            string yamlFile = Path.Combine(Directory.GetCurrentDirectory(), "Commandir.yaml");
            string yaml = File.ReadAllText(yamlFile);
            s_commandDataProvider = new YamlCommandDataProvider(yaml);

            try
            {
                await BuildCommandLine()
                .UseHost(host => {
                    host.UseSerilog(logger);
                    host.ConfigureServices(services =>
                    {
                        services.AddCommandirBaseServices();
                        services.AddCommandirDataServices(s_commandDataProvider);
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
            var rootCommandData = s_commandDataProvider.GetRootCommandData();
            var rootCommand = YamlCommandBuilder.Build(rootCommandData);

            rootCommand.SetHandlers(async services =>
            {
                var dynamicCommandProvider = services.GetRequiredService<IDynamicCommandDataProvider>();
                var dynamicCommandData = dynamicCommandProvider.GetCommandData();
                
                var cancellationTokenProvider = services.GetRequiredService<ICancellationTokenProvider>();
                var cancellationToken = cancellationTokenProvider.GetCancellationToken();

                var commandDataProvider = services.GetRequiredService<ICommandDataProvider<YamlCommandData>>();
                var commandData = commandDataProvider.GetCommandData(dynamicCommandData!.Path);

                var parameterProvider = services.GetRequiredService<IParameterProvider>();
                parameterProvider.AddOrUpdateParameters(commandData!.Parameters!);
                parameterProvider.AddOrUpdateParameters(dynamicCommandData!.Parameters!);

                var actionProvider = services.GetRequiredService<IActionProvider>();
                var action = actionProvider.GetAction(commandData.Type!);
                if(action == null)
                    throw new Exception($"Failed to find action: {commandData.Type!}");
                
                var result = await action.ExecuteAsync(services); 
                s_logger?.LogInformation("Result: {Result}", result);

            }, exception => 
            {
                s_logger?.LogError(exception, "");
            });
            return new CommandLineBuilder(rootCommand);
        }

        private static void HandleException(Exception e)
        {
            s_logger?.LogError(e, "Exception: ");
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