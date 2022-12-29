using Commandir.Commands;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Commandir.Tests;

public class CommandExecutionTests : TestsBase
{
    // private string GetCommands()
    // {
    //     return $@"---
    //         commands:
    //            - name: command-tests
    //              executor: test
    //              parameters:
    //                 greeting: Hello
    //                 message: {{greeting}} {{name}}
    //              arguments:
    //                 - name: name
    //                   description: The user's name
    //              options:
    //                 - name: greeting
    //                   description: The greeting
    //                   required: false
    //     ";
    // }

    private string GetCommands()
    {
        return $@"---
            commands:
               - name: execution-tests
                 executor: test
                 parameters:
                    name: Default
                    # Note: The value of a parameter cannot start with {{}} because it triggers a yaml parse exception. 
                    message: Hello {{{{name}}}}
                 arguments:
                    - name: name
                      description: The user's name.
                 options:
                    -  name: name
                       description: An override for the user's name.
                       required: false
        ";
    }

    [Fact]
    public async Task ArgumentOverridesParameter()
    { 
        string yaml = GetCommands();
        ICommandExecutionResult result = await RunCommandAsync(yaml, new [] {"execution-tests", "World"});
        SuccessfulCommandExecution? success = result as SuccessfulCommandExecution;
        Assert.NotNull(success);
        string? commandResult = success!.Results.First() as string;
        Assert.Equal("Hello World", commandResult);
    }

     [Fact]
    public async Task OptionOverridesArgument()
    {
        string yaml = GetCommands();
        ICommandExecutionResult result = await RunCommandAsync(yaml, new [] {"execution-tests", "World", "--name", "Universe"});
        SuccessfulCommandExecution? success = result as SuccessfulCommandExecution;
        Assert.NotNull(success);
        string? commandResult = success!.Results.First() as string;
        Assert.Equal("Hello Universe", commandResult);
    }
}