using Commandir.Commands;
using Commandir.Interfaces;
using Commandir.Services;
using Commandir.Yaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Commandir.Tests;

public class IntegrationTests
{
    private async Task RunCommandAsync(string tempFile, string yaml, string[] commandLineArgs, string expectedCommandResult)
    {
        var commandDataProvider = new YamlCommandDataProvider(yaml);
        var rootCommandData = commandDataProvider.GetRootCommandData();
        var rootCommand = YamlCommandBuilder.Build(rootCommandData!);

        rootCommand.SetHandlers(async services =>
            {
                var result = await CommandExecutor.ExecuteAsync(services);
            }, exception => 
            {
                Console.WriteLine($"Exception = {exception}");
            });


        var parser = new CommandLineBuilder(rootCommand)
                        .UseHost(host => 
                        {
                            host.ConfigureServices(services =>
                            {
                                services.AddCommandirBaseServices();
                                services.AddCommandirDataServices(commandDataProvider);
                            });
                        })
                        .Build();

        await parser.InvokeAsync(commandLineArgs);

        string fileContents = File.ReadAllText(tempFile).TrimEnd('\n');
        Assert.Equal(expectedCommandResult, fileContents);
    }

    private string GetYaml(string tempFile)
    {
        return $@"---
            commands:
               - name: greet
                 action: commandir.actions.run
                 parameters:
                    greeting: Hello
                    command: echo {{{{greeting}}}} {{{{name}}}} > {tempFile}
                 arguments:
                    - name: name
                      description: The user's name
                 options:
                    -  name: greeting
                       description: The greeting
                       required: false
               - name: hello
                 parameters:
                         command: echo Hello World > {tempFile}
                 commands:
                    - name: world
                      action: commandir.actions.run
        ";
    }

    [Fact]
    public async Task ArgumentTest()
    {
        string tempFile = Path.GetTempFileName(); 
        string yaml = GetYaml(tempFile);
        await RunCommandAsync(tempFile, yaml, new [] {"greet", "World"}, "Hello World");
        File.Delete(tempFile);
    }

    [Fact]
    public async Task OptionTest()
    {
        string tempFile = Path.GetTempFileName(); 
        string yaml = GetYaml(tempFile);
        await RunCommandAsync(tempFile, yaml, new [] {"greet", "World", "--greeting", "Hey"}, "Hey World");
        File.Delete(tempFile);
    }

    [Fact]
    public async Task SubCommandTest()
    {
        string tempFile = Path.GetTempFileName(); 
        string yaml = GetYaml(tempFile);
        await RunCommandAsync(tempFile, yaml, new [] {"hello", "world"}, "Hello World");
        File.Delete(tempFile);
    }
}