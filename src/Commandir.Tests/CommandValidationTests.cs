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
               - name: validation-tests
                 parameters:
                    executable: true
                 commands:
                    - name: build
                      parameters:
                         command: echo Built > {fileName}

        ";
    }

    [Fact]
    public async Task InvalidInternalCommandName()
    {
        using var file1 = new TempFile(); 
        string yaml = GetCommands(file1.FileName);
        var result = await RunCommandAsync(yaml, new [] {"validation-tsts"});
        var failure = result as FailedCommandExecution;
        Assert.NotNull(failure);
        Assert.Equal("Unrecognized command or argument 'validation-tsts'.", failure!.Error);
    }

    [Fact]
    public async Task ValidInternalCommandWithInvalidArgument()
    {
        using var file1 = new TempFile();
        string yaml = GetCommands(file1.FileName);
        var result = await RunCommandAsync(yaml, new [] {"validation-tests", "build", "foo"});
        var failure = result as FailedCommandExecution;
        Assert.NotNull(failure);
        Assert.Equal("Unrecognized command or argument 'foo'.", failure!.Error);
    }

    [Fact]
    public async Task ValidInternalCommandNameWithInvalidSubCommandName()
    {
        using var file1 = new TempFile(); 
        string yaml = GetCommands(file1.FileName);
        var result = await RunCommandAsync(yaml, new [] {"validation-tests", "bld"});
        var failure = result as FailedCommandExecution;
        Assert.NotNull(failure);
        Assert.Equal("Unrecognized command or argument 'bld'.", failure!.Error);
    }
}