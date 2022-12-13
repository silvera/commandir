using Commandir.Commands;
using Commandir.Yaml;
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
        private static Microsoft.Extensions.Logging.ILoggerFactory? s_loggerFactory;
        private static Microsoft.Extensions.Logging.ILogger? s_logger;
        private static YamlCommandDataProvider? s_commandDataProvider;
        private static CommandExecutor? s_commandExecutor;

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

            s_loggerFactory = new LoggerFactory().AddSerilog(logger);
            s_logger = s_loggerFactory.CreateLogger<Program>();

            string yamlFile = Path.Combine(Directory.GetCurrentDirectory(), "Commandir.yaml");
            string yaml = File.ReadAllText(yamlFile);
            s_commandDataProvider = new YamlCommandDataProvider(yaml);

            s_commandExecutor = new CommandExecutor(s_loggerFactory, s_commandDataProvider);

            try
            {
                await BuildCommandLine()
                .AddMiddleware(async (context, next) =>
                {
                    try
                    {
                        var result = await s_commandExecutor!.ExecuteAsync(context);
                        if(result is EmptyCommandExecutionResult)
                        {
                            await next(context);
                        }
                    }
                    catch(Exception e)
                    {
                        s_logger.LogCritical("{ExceptionType}: {Message}", e.GetType().Name,  e.Message);
                    }
                })
                .UseHost(host => {
                    host.UseSerilog(logger);
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
            return new CommandLineBuilder(rootCommand);
        }
    }
}