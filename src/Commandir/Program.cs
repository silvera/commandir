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
using System.CommandLine.Parsing;


namespace Commandir
{
    public class Program
    { 
        private static Microsoft.Extensions.Logging.ILogger? s_logger;
        private static YamlCommandDataProvider? s_commandDataProvider;

        public static async Task Main(string[] args)
        {
            Serilog.Events.LogEventLevel commandirLogLevel = Serilog.Events.LogEventLevel.Warning;
            if(args.Length > 0)
            {
                if(string.Equals(args[0], "--verbose", StringComparison.OrdinalIgnoreCase))
                    commandirLogLevel = Serilog.Events.LogEventLevel.Information;
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
            var rootCommandData = s_commandDataProvider?.GetRootCommandData();
            var rootCommand = YamlCommandBuilder.Build(rootCommandData!);

            rootCommand.SetHandlers(async services =>
            {
                var dynamicCommandProvider = services.GetRequiredService<IDynamicCommandDataProvider>();
                var dynamicCommandData = dynamicCommandProvider.GetCommandData();
                if(dynamicCommandData == null)
                    throw new Exception("Failed to obtain dynamic command data");
                
                var cancellationTokenProvider = services.GetRequiredService<ICancellationTokenProvider>();
                var cancellationToken = cancellationTokenProvider.GetCancellationToken();
                if(cancellationToken == null)
                    throw new Exception($"Failed to obtain cancellation token.");

                var commandDataProvider = services.GetRequiredService<ICommandDataProvider<YamlCommandData>>();
                var commandData = commandDataProvider.GetCommandData(dynamicCommandData!.Path);
                if(commandData == null)
                    throw new Exception($"Failed to find command data data using path: {dynamicCommandData!.Path}");

                var parameterProvider = services.GetRequiredService<IParameterProvider>();
                parameterProvider.AddOrUpdateParameters(commandData.Parameters);
                parameterProvider.AddOrUpdateParameters(dynamicCommandData.Parameters);

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
    }
}