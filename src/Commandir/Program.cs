using Commandir.Commands;
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
        public static async Task Main(string[] args)
        {
            Serilog.Events.LogEventLevel commandirLogLevel = Serilog.Events.LogEventLevel.Warning;
            if(args.Length > 0)
            {
                if(string.Equals(args[0], "--verbose", StringComparison.OrdinalIgnoreCase))
                    commandirLogLevel = Serilog.Events.LogEventLevel.Information;
            }

            Serilog.Core.Logger seriLogger = new LoggerConfiguration()
                .MinimumLevel.Override("Commandir", commandirLogLevel)
                .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Error)
                .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Error)
                .WriteTo.Console(outputTemplate: "{SourceContext}: {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            ILoggerFactory loggerFactory = new LoggerFactory().AddSerilog(seriLogger);
            Microsoft.Extensions.Logging.ILogger logger = loggerFactory.CreateLogger<Program>();

            try
            {
                string yamlFile = Path.Combine(Directory.GetCurrentDirectory(), "Commandir.yaml");
                string yaml = File.ReadAllText(yamlFile);
                
                YamlCommandBuilder commandBuilder = new YamlCommandBuilder(yaml);
                
                CommandExecutor commandExecutor = new CommandExecutor(loggerFactory);
                CommandWithData rootCommand = commandBuilder.Build();
                
                await new CommandLineBuilder(rootCommand)
                .AddMiddleware(async (context, next) =>
                {
                    try
                    {
                        var result = await commandExecutor.ExecuteAsync(context);
                        if(result is FailedCommandExecution)
                        {
                            await next(context);
                        }
                    }
                    catch(Exception e)
                    {
                        logger.LogCritical("{ExceptionType}: {Message}", e.GetType().Name,  e.Message);
                    }
                })
                .UseHost(host => {
                    host.UseSerilog(seriLogger);
                })
                .UseDefaults()
                .Build()
                .InvokeAsync(args);
            }
            catch(Exception e)
            {
                logger.LogCritical("{ExceptionType}: {Message}", e.GetType().Name,  e.Message);
            }
        }
    }
}