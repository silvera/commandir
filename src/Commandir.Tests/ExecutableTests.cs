using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Commandir.Tests;

public class ExecutableTests : TestsBase
{
    private string GetExecutableTestsYaml(string fileName, bool executable)
    {
        return $@"---
            commands:
               - name: hello
                 parameters:
                    executable: {executable}
                 options:
                    - name: executable
                      description: Determines whether or not to execute child commands
                 commands:
                    - name: world
                      executor: commandir.executors.run
                      parameters:
                         command: echo Hello World > {fileName}

        ";
    }

    [Theory]
    [InlineData(true, "Hello World")]
    [InlineData(false, "")]
    public async Task ExecutableParameterTest(bool executable, string expectedCommandOutput)
    {
        string tempFile = Path.GetTempFileName(); 
        string yaml = GetExecutableTestsYaml(tempFile, executable: executable);
        await RunCommandAsync(yaml, new [] {"hello"});
        AssertCommandOutput(tempFile, expectedCommandOutput);
        File.Delete(tempFile);
    }

    [Theory]
    [InlineData(true, "Hello World")]
    [InlineData(false, "")]
    public async Task ExecutableOptionTest(bool executable, string expectedCommandOutput)
    {
        string tempFile = Path.GetTempFileName(); 
        string yaml = GetExecutableTestsYaml(tempFile, executable: true);
        string executableStr = $"{executable}";
        var result = await RunCommandAsync(yaml, new [] {"hello", "--executable", executableStr});
        AssertCommandOutput(tempFile, expectedCommandOutput);
        File.Delete(tempFile);
    }
}