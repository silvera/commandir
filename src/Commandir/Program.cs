﻿using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using FluentResults;

namespace Commandir
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                await BuildCommandLine()
                .UseHost(_ => Host.CreateDefaultBuilder(args),
                    host =>
                    { 
                        host.ConfigureServices(services => 
                        {
                            services.AddActions();
                        });
                    })
                .UseDefaults()
                .Build()
                .InvokeAsync(args);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private static async Task HandleAsync(IHost host)
        {            
            IActionContextProvider provider = host.Services.GetRequiredService<IActionContextProvider>();
            CancellationToken cancellationToken = provider.GetCancellationToken();
            foreach(Action action in provider.GetActions())
            {
                await action.ExecuteAsync(cancellationToken);
            }
        }

        private static CommandLineBuilder BuildCommandLine()
        {
            string currentDirectory = Directory.GetCurrentDirectory(); 
            string yamlFilePath = Path.Combine(currentDirectory, "Commandir.yaml");
            if(!File.Exists(yamlFilePath))
                throw new FileNotFoundException($"No Commandir.yaml file found in {currentDirectory}", "Commandir.yaml");

            var yamlFileReader = new StreamReader(yamlFilePath);
            YamlCommandBuilder commandBuilder = new YamlCommandBuilder(yamlFileReader);
            Command command = commandBuilder.Build(HandleAsync);
            return new CommandLineBuilder(command);
        }
    }
}
