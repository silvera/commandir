using Commandir.Commands;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;

namespace Commandir
{
    public class Program
    { 
        public static async Task Main(string[] args)
        {
            // Used to control the Commandir logging level based on command-line arguments e.g. --verbose.
            LoggingLevelSwitch commandirLevelSwitch = new LoggingLevelSwitch(Serilog.Events.LogEventLevel.Warning);

            Serilog.Core.Logger seriLogger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(commandirLevelSwitch)
                .MinimumLevel.Override("System", LogEventLevel.Error)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Error)
                .WriteTo.Console(outputTemplate: "{SourceContext}: {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            ILoggerFactory loggerFactory = new LoggerFactory().AddSerilog(seriLogger);
            Microsoft.Extensions.Logging.ILogger logger = loggerFactory.CreateLogger<Program>();

            try
            {
                string yamlFile = Path.Combine(Directory.GetCurrentDirectory(), "Commandir.yaml");
                string yaml = File.ReadAllText(yamlFile);
                
                YamlCommandBuilder commandBuilder = new YamlCommandBuilder(yaml);
                CommandWithData rootCommand = commandBuilder.Build();

                await new CommandLineBuilder(rootCommand)
                .AddMiddleware(async (context, next) =>
                {
                    try
                    {
                        // Set the Commandir logging level. 
                        SetCommandirLogLevel(context, rootCommand, commandirLevelSwitch);

                        await new CommandExecutor(loggerFactory).ExecuteAsync(context);
                    }
                    catch(CommandValidationException)
                    {
                        await next(context);
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

        private static void SetCommandirLogLevel(InvocationContext invocationContext, CommandWithData command, LoggingLevelSwitch logLevelSwitch)
        {
            Option? verboseOption = command.Options.FirstOrDefault(o => o.Name == "verbose");
            if(verboseOption is null)
                throw new Exception("Failed to find `verbose` option");

            object? verboseLoggingObj = invocationContext.ParseResult.GetValueForOption(verboseOption);
            if(verboseLoggingObj is not null)
            {
                bool verboseLogging = Convert.ToBoolean(verboseLoggingObj); 
                if(verboseLogging)
                {
                    logLevelSwitch.MinimumLevel = LogEventLevel.Verbose;
                }
            }
        }
    }
}