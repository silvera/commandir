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
    class Program
    {
        static async Task Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true)
            .Build();


            var logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
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

            CommandExecutor commandExecutor = new CommandExecutor(loggerFactory);
            CommandBuilder commandBuilder = new CommandBuilder(rootDefinition, commandExecutor.ExecuteAsync, loggerFactory);
            CommandLineCommand rootCommand = commandBuilder.Build(); 
            return new CommandLineBuilder(rootCommand);
        }
    }
}