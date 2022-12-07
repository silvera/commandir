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
                // .AddMiddleware(async (context, next) =>
                // {
                //     if (context.ParseResult.Directives.Contains("just-say-hi"))
                //     {
                //         context.Console.WriteLine("Hi!");
                //     }
                //     else
                //     {
                //         await next(context);
                //     }
                // })
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
                var result = await CommandExecutor.ExecuteAsync(services); 
                s_logger?.LogInformation("Result: {Result}", result);

            }, exception => 
            {
                s_logger?.LogError(exception, "");
            });
            return new CommandLineBuilder(rootCommand);
        }
    }
}