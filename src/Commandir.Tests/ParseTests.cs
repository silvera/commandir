using Commandir.Commands;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Commandir.Tests;

public class ParseTests : TestsBase
{
    private string GetParseTestsYaml(string fileName)
    {
        return $@"---
            commands:
               - name: parse
                 parameters:
                    executable: true
                 commands:
                    - name: world
                      executor: commandir.executors.run
                      parameters:
                         command: echo Hello World > {fileName}

        ";
    }

    [Fact]
    public async Task InvalidInternalCommandName()
    {
        string tempFile = Path.GetTempFileName(); 
        string yaml = GetParseTestsYaml(tempFile);
        var result = await RunCommandAsync(yaml, new [] {"helo"});
        var failure = result as FailedCommandExecution;
        Assert.Equal("Unrecognized command or argument 'helo'.", failure.Error);
    }

    [Fact]
    public async Task ValidInternalCommandName()
    {
        string tempFile = Path.GetTempFileName(); 
        string yaml = GetParseTestsYaml(tempFile);
        var result = await RunCommandAsync(yaml, new [] {"parse", "world"});
        Assert.True(result is SuccessfulCommandExecution);
    } 

    [Fact]
    public async Task ValidInternalCommandWithInvalidArgument()
    {
        string tempFile = Path.GetTempFileName(); 
        string yaml = GetParseTestsYaml(tempFile);
        var result = await RunCommandAsync(yaml, new [] {"parse", "world", "foo"});
        var failure = result as FailedCommandExecution;
        Assert.Equal("Unrecognized command or argument 'foo'.", failure.Error);
    }

    [Fact]
    public async Task ValidInternalCommandNameWithInvalidSubCommandName()
    {
        string tempFile = Path.GetTempFileName(); 
        string yaml = GetParseTestsYaml(tempFile);
        var result = await RunCommandAsync(yaml, new [] {"parse", "wrld"});
        var failure = result as FailedCommandExecution;
        Assert.Equal("Unrecognized command or argument 'wrld'.", failure.Error);
    }
}