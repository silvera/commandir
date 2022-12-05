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
                var dynamicCommandProvider = services.GetRequiredService<IDynamicCommandDataProvider>();
                var dynamicCommandData = dynamicCommandProvider.GetCommandData();
                
                var cancellationTokenProvider = services.GetRequiredService<ICancellationTokenProvider>();
                var cancellationToken = cancellationTokenProvider.GetCancellationToken();

                var commandDataProvider = services.GetRequiredService<ICommandDataProvider<YamlCommandData>>();
                var commandData = commandDataProvider.GetCommandData(dynamicCommandData!.Path!);

                var parameterProvider = services.GetRequiredService<IParameterProvider>();
                parameterProvider.AddOrUpdateParameters(commandData!.Parameters!);
                parameterProvider.AddOrUpdateParameters(dynamicCommandData!.Parameters!);

                var actionProvider = services.GetRequiredService<IActionProvider>();
                var actionType = commandData.Action!;
                var action = actionProvider.GetAction(actionType);
                if(action == null)
                    throw new Exception($"Failed to find action: {actionType}");

                await action.ExecuteAsync(services);
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

    [Fact]
    public async Task ArgumentTest()
    {
        string tempFile = Path.GetTempFileName(); 
        string yaml = $@"---
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
        ";

        await RunCommandAsync(tempFile, yaml, new [] {"greet", "World"}, "Hello World");
        File.Delete(tempFile);
    }

    [Fact]
    public async Task OptionTest()
    {
        string tempFile = Path.GetTempFileName(); 
        string yaml = $@"---
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
        ";

        await RunCommandAsync(tempFile, yaml, new [] {"greet", "World", "--greeting", "Hey"}, "Hey World");
        File.Delete(tempFile);
    }

    [Fact]
    public async Task SubCommandTest()
    {
        string tempFile = Path.GetTempFileName(); 
        string yaml = $@"---
            commands:
               - name: hello
                 action: commandir.actions.run
                 commands:
                    - name: world
                      action: commandir.actions.run
                      parameters:
                         command: echo Hello World > {tempFile}
        ";

        await RunCommandAsync(tempFile, yaml, new [] {"hello", "world"}, "Hello World");
        File.Delete(tempFile);
    }
}