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
                 executor: test
                 parameters:
                    executable: true
                 commands:
                    - name: build
                      parameters:
                         message: Built > {fileName}

        ";
    }

    [Fact]
    public async Task InvalidInternalCommandName()
    {
        using var file1 = new TempFile(); 
        string yaml = GetCommands(file1.FileName);

        CommandValidationException exception = await Assert.ThrowsAsync<CommandValidationException>(() =>
        {
            return RunCommandAsync(yaml, new [] {"validation-tsts"});
        });
        Assert.Equal("Unrecognized command or argument 'validation-tsts'.", exception.Message);
    }

    [Fact]
    public async Task ValidInternalCommandWithInvalidArgument()
    {
        using var file1 = new TempFile();
        string yaml = GetCommands(file1.FileName);
        
        CommandValidationException exception = await Assert.ThrowsAsync<CommandValidationException>(() =>
        {
            return RunCommandAsync(yaml, new [] {"validation-tests", "build", "foo"});
        });
        Assert.Equal("Unrecognized command or argument 'foo'.", exception.Message);
    }

    [Fact]
    public async Task ValidInternalCommandNameWithInvalidSubCommandName()
    {
        using var file1 = new TempFile(); 
        string yaml = GetCommands(file1.FileName);
        
        CommandValidationException exception = await Assert.ThrowsAsync<CommandValidationException>(() =>
        {
            return RunCommandAsync(yaml, new [] {"validation-tests", "bld"});
        });
        Assert.Equal("Unrecognized command or argument 'bld'.", exception.Message);
    }
}