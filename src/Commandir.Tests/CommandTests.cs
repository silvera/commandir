using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Commandir.Tests;

public class CommandTests : TestsBase
{
    private string GetYaml(string tempFile)
    {
        return $@"---
            commands:
               - name: greet
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
               - name: command
                 parameters:
                         command: echo Hello World > {tempFile}
                 commands:
                    - name: world
                      executor: commandir.executors.run
        ";
    }

    [Fact]
    public async Task ArgumentTest()
    {
        string tempFile = Path.GetTempFileName(); 
        string yaml = GetYaml(tempFile);
        await RunCommandAsync(yaml, new [] {"greet", "World"});
        AssertCommandOutput(tempFile, "Hello World");
        File.Delete(tempFile);
    }

    [Fact]
    public async Task OptionTest()
    {
        string tempFile = Path.GetTempFileName(); 
        string yaml = GetYaml(tempFile);
        await RunCommandAsync(yaml, new [] {"greet", "World", "--greeting", "Hey"});
        AssertCommandOutput(tempFile, "Hey World");
        File.Delete(tempFile);
    }

    [Fact]
    public async Task SubCommandTest()
    {
        string tempFile = Path.GetTempFileName(); 
        string yaml = GetYaml(tempFile);
        await RunCommandAsync(yaml, new [] {"command", "world"});
        AssertCommandOutput(tempFile, "Hello World");
        File.Delete(tempFile);
    }
}