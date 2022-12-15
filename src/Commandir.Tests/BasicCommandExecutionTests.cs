using System.Threading.Tasks;
using Xunit;

namespace Commandir.Tests;

public class BasicCommandExecutionTests : TestsBase
{
    private string GetCommands(string file1)
    {
        return $@"---
            commands:
               - name: basic-commands
                 executor: commandir.executors.run
                 parameters:
                    greeting: Hello
                    command: echo {{{{greeting}}}} {{{{name}}}} > {file1}
                 arguments:
                    - name: name
                      description: The user's name
                 options:
                    -  name: greeting
                       description: The greeting
                       required: false
        ";
    }

    [Fact]
    public async Task RequiredArgument()
    {
        using var file1 = new TempFile(); 
        string yaml = GetCommands(file1.FileName);
        await RunCommandAsync(yaml, new [] {"basic-commands", "World"});
        Assert.True(file1.ContentEqual("Hello World"));
    }

    [Fact]
    public async Task OptionOverridesParameter()
    {
        using var file1 = new TempFile(); 
        string yaml = GetCommands(file1.FileName);
        await RunCommandAsync(yaml, new [] {"basic-commands", "World", "--greeting", "Hey"});
        Assert.True(file1.ContentEqual("Hey World"));
    }
}