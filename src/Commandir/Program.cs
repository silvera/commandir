using Commandir.Core;
using Microsoft.Extensions.Configuration;
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
        public static async Task Main(string[] args)
        {
            var logger = new LoggerConfiguration()
                .MinimumLevel.Override("Commandir", Serilog.Events.LogEventLevel.Information)
                .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Error)
                .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
                .WriteTo.Console(outputTemplate: "{SourceContext}: {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            Log.Logger = logger;
            ILoggerFactory loggerFactory = new LoggerFactory().AddSerilog(logger);

            try
            {
                await BuildCommandLine(loggerFactory)
                .UseHost(_ => Host.CreateDefaultBuilder(args)
                .UseSerilog(logger))
                .UseDefaults()
                .Build()
                .InvokeAsync(args);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private static CommandLineBuilder BuildCommandLine(ILoggerFactory loggerFactory)
        {
            CommandDefinition? rootDefinition = new CommandDefinitionBuilder()
                                .AddYamlFile(Path.Combine(Directory.GetCurrentDirectory(), "Commandir.yaml"))
                                .Build();

            CommandProvider commandProvider = new CommandProvider(loggerFactory);
            commandProvider.AddCommands(typeof(Program).Assembly);

            CommandExecutor commandExecutor = new CommandExecutor(commandProvider, loggerFactory);
            CommandBuilder commandBuilder = new CommandBuilder(rootDefinition, commandExecutor.ExecuteAsync, loggerFactory); // commandExecutor.ExecuteAsync
            CommandLineCommand rootCommand = commandBuilder.Build(); 
            return new CommandLineBuilder(rootCommand);
        }
    }
}