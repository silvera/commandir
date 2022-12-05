using Commandir.Actions;
using Commandir.Commands;
using Commandir.Interfaces;
using Commandir.Yaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Commandir.Tests;

public class RunTests
{
    // [Fact]
    // public async Task RunTest()
    // {
    //     string tempFile = Path.GetTempFileName();

    //     string yaml = $@"---
    //         commands:
    //            - name: test
    //              type: commandir.actions.run
    //              parameters:
    //                 user: World
    //                 command: echo Hello {{{{user}}}} > {tempFile}
    //     ";

    //     var rootCommand = YamlCommandParser.Parse(yaml);
    //     rootCommand.SetHandlers(async services =>
    //         {
    //            var context = CommandInvocationContext.Create(services);
    //            var executor = services.GetRequiredService<IActionExecutor>();
    //            var response = await executor.ExecuteAsync(context);

    //         }, exception => 
    //         {
    //         });

    //     var parser = new CommandLineBuilder(rootCommand)
    //             .UseHost(host => 
    //             {
    //                 host.AddCommandirServices();
    //             })
    //             .Build();

    //     await parser.InvokeAsync(new string[] { "test" });

    //     string fileContents = File.ReadAllText(tempFile).TrimEnd('\n');
    //     Assert.Equal("Hello World", fileContents);
    //     File.Delete(tempFile);
    // }

    // [Fact]
    // public async Task Integration_Test()
    // {
    //     string tempFile = Path.GetTempFileName();
            
    //     string yaml = $@"---
    //         commands:
    //            - name: test
    //              type: commandir.actions.run
    //              parameters:
    //                 user: World
    //                 command: echo Hello {{{{user}}}} > {tempFile}
    //     ";

    //     var rootCommand = YamlCommandParser.Parse(yaml);

    //     var services = new ServiceCollection()
    //                     .AddLogging(builder => builder.AddConsole())
    //                     .AddCommandirServices()
    //                     .BuildServiceProvider();

    //     var parameters = services.GetRequiredService<IParameterProvider>();
    
    //     CommandirCommand command = (CommandirCommand)rootCommand.Subcommands[0]; 
        
    //     object? userValue = command.Parameters["user"];
    //     parameters.AddOrUpdateParameter("user", userValue);
        
    //     object? commandValue = command.Parameters["command"];
    //     parameters.AddOrUpdateParameter("command", commandValue);
        
    //     var action = new Run();
    //     var request = new ActionRequest(services, cancellationToken: default);
    //     var response = await action.HandleAsync(request);

    //     string fileContents = File.ReadAllText(tempFile).TrimEnd('\n');
    //     Assert.Equal("Hello World", fileContents);
    //     File.Delete(tempFile);
    // }
}
