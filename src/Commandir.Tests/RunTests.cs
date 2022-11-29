using Commandir.Actions;
using Commandir.Commands;
using Commandir.Interfaces;
using Commandir.Yaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Commandir.Tests;

public class RunTests
{
    [Fact]
    public async Task Integration_Test()
    {
        string tempFile = Path.GetTempFileName();
            
        string yaml = $@"---
            commands:
               - name: test
                 type: commandir.actions.run
                 parameters:
                    user: World
                    command: echo Hello {{{{user}}}} > {tempFile}
        ";

        var rootCommand = YamlCommandParser.Parse(yaml);

        var services = new ServiceCollection()
                        .AddLogging(builder => builder.AddConsole())
                        .AddCommandirServices()
                        .BuildServiceProvider();

        var parameters = services.GetRequiredService<IParameterProvider>();
    
        CommandirCommand command = (CommandirCommand)rootCommand.Subcommands[0]; 
        
        object? userValue = command.Parameters["user"];
        parameters.AddOrUpdateParameter("user", userValue);
        
        object? commandValue = command.Parameters["command"];
        parameters.AddOrUpdateParameter("command", commandValue);
        
        var action = new Run();
        var request = new ActionRequest(services, cancellationToken: default);
        var response = await action.HandleAsync(request);

        string fileContents = File.ReadAllText(tempFile).TrimEnd('\n');
        Assert.Equal("Hello World", fileContents);
        File.Delete(tempFile);
    }
}
