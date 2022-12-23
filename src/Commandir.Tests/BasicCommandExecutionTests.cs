using Commandir.Commands;
using System.Threading.Tasks;
using Xunit;

namespace Commandir.Tests;

public class BasicCommandExecutionTests : TestsBase
{
    private string GetCommands(string file1)
    {
        return $@"---
            commands:
               - name: basic-tests
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
        ICommandExecutionResult result = await RunCommandAsync(yaml, new [] {"basic-tests", "World"});
        System.Console.WriteLine($"Content={file1.GetContent()}");
        Assert.True(result is SuccessfulCommandExecution);
        Assert.True(file1.ContentEqual("Hello World"));
    }

    [Fact]
    public async Task OptionOverridesParameter()
    {
        using var file1 = new TempFile(); 
        string yaml = GetCommands(file1.FileName);
        ICommandExecutionResult result = await RunCommandAsync(yaml, new [] {"basic-tests", "World", "--greeting", "Hey"});
        Assert.True(result is SuccessfulCommandExecution);
        Assert.True(file1.ContentEqual("Hey World"));
    }
}