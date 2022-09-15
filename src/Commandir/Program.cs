using Microsoft.Extensions.Hosting;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;

using Commandir.Core;

namespace Commandir
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                await BuildCommandLine()
                .UseHost(_ => Host.CreateDefaultBuilder(args))
                .UseDefaults()
                .Build()
                .InvokeAsync(args);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private static CommandLineBuilder BuildCommandLine()
        {
            string currentDirectory = Directory.GetCurrentDirectory(); 
            string yamlFilePath = Path.Combine(currentDirectory, "Commandir.yaml");
            if(!File.Exists(yamlFilePath))
                throw new FileNotFoundException($"No Commandir.yaml file found in {currentDirectory}", "Commandir.yaml");

            string yaml = File.ReadAllText(yamlFilePath);
            Core.CommandData rootData  = new YamlCommandDataBuilder(yaml).Build();
            CommandLineCommand rootCommand = new CommandBuilder(rootData, CommandExecutor.ExecuteAsync).Build(); 
            return new CommandLineBuilder(rootCommand);
        }
    }
}
