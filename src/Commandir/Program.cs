using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;

using Serilog;
using Serilog.Extensions.Hosting;
using Serilog.Settings.Configuration;

using Commandir.Core;

namespace Commandir
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", true)
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
                .UseSerilog())
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
            string currentDirectory = Directory.GetCurrentDirectory(); 
            string yamlFilePath = Path.Combine(currentDirectory, "Commandir.yaml");
            if(!File.Exists(yamlFilePath))
                throw new FileNotFoundException($"No Commandir.yaml file found in {currentDirectory}", "Commandir.yaml");

            var logger = loggerFactory.CreateLogger<Program>();
            logger.LogInformation("Loading Definitions: {YamlFilePath}", yamlFilePath);
            string yaml = File.ReadAllText(yamlFilePath);
        
            YamlCommandDataBuilder commandDataBuilder = new YamlCommandDataBuilder(yaml); 
            CommandData rootData  = commandDataBuilder.Build();
        
            CommandExecutor commandExecutor = new CommandExecutor(loggerFactory);
            CommandBuilder commandBuilder = new CommandBuilder(rootData, commandExecutor.ExecuteAsync, loggerFactory);
            CommandLineCommand rootCommand = commandBuilder.Build(); 
            return new CommandLineBuilder(rootCommand);
        }
    }
}
