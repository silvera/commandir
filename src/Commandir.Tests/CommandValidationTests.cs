using Commandir.Commands;
using System.Threading.Tasks;
using Xunit;

namespace Commandir.Tests;

public class CommandValidationTests : TestsBase
{
    private string GetCommands(string fileName)
    {
        return $@"---
            commands:
               - name: failed-tests
                 parameters:
                    executable: true
                 commands:
                    - name: build
                      executor: commandir.executors.run
                      parameters:
                         command: echo Built > {fileName}

        ";
    }

    [Fact]
    public async Task InvalidInternalCommandName()
    {
        using var file1 = new TempFile(); 
        string yaml = GetCommands(file1.FileName);
        var result = await RunCommandAsync(yaml, new [] {"failed-tsts"});
        var failure = result as FailedCommandExecution;
        Assert.Equal("Unrecognized command or argument 'failed-tsts'.", failure.Error);
    }

    [Fact]
    public async Task ValidInternalCommandWithInvalidArgument()
    {
        using var file1 = new TempFile();
        string yaml = GetCommands(file1.FileName);
        var result = await RunCommandAsync(yaml, new [] {"failed-tests", "build", "foo"});
        var failure = result as FailedCommandExecution;
        Assert.Equal("Unrecognized command or argument 'foo'.", failure.Error);
    }

    [Fact]
    public async Task ValidInternalCommandNameWithInvalidSubCommandName()
    {
        using var file1 = new TempFile(); 
        string yaml = GetCommands(file1.FileName);
        var result = await RunCommandAsync(yaml, new [] {"failed-tests", "bld"});
        var failure = result as FailedCommandExecution;
        Assert.Equal("Unrecognized command or argument 'bld'.", failure.Error);
    }
}