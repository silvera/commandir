using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Commandir.Tests;

public class RecurseTests : TestsBase
{
    private string GetRecurseTestsYaml(string tempFile, bool recurse)
    {
        return $@"---
            commands:
               - name: hello
                 parameters:
                    recurse: {recurse}
                 options:
                    - name: recurse
                      description: Determines whether or not to execute child commands
                 commands:
                    - name: world
                      executor: commandir.executors.run
                      parameters:
                         command: echo Hello World > {tempFile}

        ";
    }

    [Theory]
    [InlineData(true, true, "Hello World")]
    [InlineData(false, false, "")]
    public async Task RecurseParameterTest(bool recurse, bool expectedHasResult, string expectedCommandOutput)
    {
        string tempFile = Path.GetTempFileName(); 
        string yaml = GetRecurseTestsYaml(tempFile, recurse: recurse);
        var result = await RunCommandAsync(yaml, new [] {"hello"});
        Assert.Equal(expectedHasResult, result.HasResult);
        AssertCommandOutput(tempFile, expectedCommandOutput);
        File.Delete(tempFile);
    }

    [Theory]
    [InlineData(true, true, "Hello World")]
    [InlineData(false, false, "")]
    public async Task RecurseOptionTest(bool recurse, bool expectedHasResult, string expectedCommandOutput)
    {
        string tempFile = Path.GetTempFileName(); 
        string yaml = GetRecurseTestsYaml(tempFile, recurse: true);
        string recurseStr = $"{recurse}";
        var result = await RunCommandAsync(yaml, new [] {"hello", "--recurse", recurseStr});
        Assert.Equal(expectedHasResult, result.HasResult);
        AssertCommandOutput(tempFile, expectedCommandOutput);
        File.Delete(tempFile);
    }
}