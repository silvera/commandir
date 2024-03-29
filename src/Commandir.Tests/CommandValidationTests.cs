using Commandir.Commands;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Commandir.Tests;

public class CommandValidationTests : TestsBase
{
    private string GetCommands()
    {
        return $@"---
            commands:
               - name: validation-tests
                 executor: test
                 commands:
                    - name: build
                      parameters:
                         message: Built

        ";
    }

    [Fact]
    public async Task InvalidInternalCommandName()
    {
        string yaml = GetCommands();
        CommandValidationException exception = await Assert.ThrowsAsync<CommandValidationException>(() =>
        {
            return RunCommandAsync(yaml, new [] {"validation-tsts"});
        });
        Assert.Equal("Unrecognized command or argument 'validation-tsts'.", exception.Message);
    }

    [Fact]
    public async Task ValidInternalCommandWithInvalidArgument()
    {
        string yaml = GetCommands();
        CommandValidationException exception = await Assert.ThrowsAsync<CommandValidationException>(() =>
        {
            return RunCommandAsync(yaml, new [] {"validation-tests", "build", "foo"});
        });
        Assert.Equal("Unrecognized command or argument 'foo'.", exception.Message);
    }

    [Fact]
    public async Task ValidInternalCommandNameWithInvalidSubCommandName()
    {
        string yaml = GetCommands();
        CommandValidationException exception = await Assert.ThrowsAsync<CommandValidationException>(() =>
        {
            return RunCommandAsync(yaml, new [] {"validation-tests", "bld"});
        });
        Assert.Equal("Unrecognized command or argument 'bld'.", exception.Message);
    }

    [Fact]
    public async Task NoCommandLineArguments()
    {
        string yaml = GetCommands();
        CommandValidationException exception = await Assert.ThrowsAsync<CommandValidationException>(() =>
        {
            return RunCommandAsync(yaml, Array.Empty<string>());
        });
        Assert.Equal("Required command was not provided.", exception.Message);
    }
}