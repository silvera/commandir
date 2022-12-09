using Commandir.Commands;
using Commandir.Services;
using Commandir.Yaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
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

        var loggerFactory = new NullLoggerFactory();
        var commandExecutor = new CommandExecutor2(loggerFactory, commandDataProvider);

        var parser = new CommandLineBuilder(rootCommand)
                .AddMiddleware(async (context, next) =>
                {
                    var command = context.ParseResult.CommandResult.Command; 
                    if (command.Subcommands.Count == 0)
                    {
                        await commandExecutor!.ExecuteAsync(context);
                    }
                    else
                    {
                        await next(context);
                    }
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
                 executor: commandir.executors.run
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
                      executor: commandir.executors.run
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