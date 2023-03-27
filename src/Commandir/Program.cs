using Commandir.Commands;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
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
            var exceptionLogger = new ExceptionLogger();
            var commandirLogger = new CommandirLogger();

            try
            {
                string currentDirectory = Directory.GetCurrentDirectory();
                string yamlFile = Path.Combine(currentDirectory, "Commandir.yaml");
                if(!File.Exists(yamlFile))
                {
                    // Manually check the command line arguments for the init flag beause System.CommandLine has not yet processed them. 
                    if(args.Contains("--init") || args.Contains("-i"))
                    {
                        await CreateNewCommandirFileAsync(commandirLogger);
                        return;
                    }
                    else
                    {
                        throw new MissingCommandirFileException();
                    }
                }
                    
                string yaml = File.ReadAllText(yamlFile);
                
                YamlCommandBuilder commandBuilder = new YamlCommandBuilder(yaml);
                CommandWithData rootCommand = commandBuilder.Build();

                await new CommandLineBuilder(rootCommand)
                .AddMiddleware(async (context, next) =>
                {
                    try
                    {
                        // Set the log level before invoking the command.
                        commandirLogger.SetLogLevel(context, rootCommand);
                        
                        await new CommandExecutor(commandirLogger.Logger).ExecuteAsync(context);
                    }
                    catch(CommandValidationException)
                    {
                        // Triggers the default 'invalid' command processing logic.
                        await next(context);
                    }
                    catch(Exception e)
                    {
                        exceptionLogger.LogException(e);
                    }
                })
                .UseHost(host => {
                    host.UseSerilog(commandirLogger.Logger);
                })
                .UseDefaults()
                .Build()
                .InvokeAsync(args);
            }
            catch(Exception e)
            {
                exceptionLogger.LogException(e);
            }
        }

        private static async Task CreateNewCommandirFileAsync(CommandirLogger logger)
        { 
            string yamlFileContents = $@"---
name: Commandir
description: Commandir Starter File
commands:
   - name: hello
     description: Prints 'Hello World!'
     parameters:
        run: echo ""Hello World!""
            ";

            string currentDirectory = Directory.GetCurrentDirectory();
            string yamlFile = Path.Combine(currentDirectory, "Commandir.yaml"); 
            await File.WriteAllTextAsync(yamlFile, yamlFileContents);
            logger.Logger.Warning("Created a new Commandir.yaml file in the current directory ({CurrentDirectory}) ", currentDirectory);
        }
    }

    internal sealed class MissingCommandirFileException : Exception
    {
        public MissingCommandirFileException()
            : base($"Could not find a Commandir.yaml file in the current directory ({Directory.GetCurrentDirectory()}). Run Commandir from a directory containing a Commandir.yaml file or run Commandir with the --init flag to create a new Commandir.yaml file in the current directory.")
        {
        }
    }

    internal sealed class ExceptionLogger
    {
        private readonly Serilog.Core.Logger _logger;
        public ExceptionLogger()
        {
            _logger = new LoggerConfiguration()
                .MinimumLevel.Override("System", LogEventLevel.Error)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Error)
                .WriteTo.Console(
                    theme: ConsoleTheme.None,
                    outputTemplate: "Commandir: {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
        }

        public void LogException(Exception e)
        {
            _logger.Fatal("{ExceptionType}: {Message}", e.GetType().Name,  e.Message);
        }
    }

    internal sealed class CommandirLogger
    {
        private readonly Serilog.Core.Logger _logger;
        private readonly  LoggingLevelSwitch _logLevel;

        public Serilog.ILogger Logger => _logger;

        public CommandirLogger()
        {
            // Used to control the Commandir logging level based on command-line arguments e.g. --verbose.
            _logLevel = new LoggingLevelSwitch(Serilog.Events.LogEventLevel.Warning);

            _logger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(_logLevel)
                .MinimumLevel.Override("System", LogEventLevel.Error)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Error)
                .WriteTo.Console(
                    theme: ConsoleTheme.None,
                    outputTemplate: "{SourceContext}: {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
        }

        public void SetLogLevel(InvocationContext invocationContext, CommandWithData command)
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
                    _logLevel.MinimumLevel = LogEventLevel.Verbose;
                }
            }
        }
    }
}