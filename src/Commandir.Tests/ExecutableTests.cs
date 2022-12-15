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
               - name: executable
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
        await RunCommandAsync(yaml, new [] {"executable"});
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
        await RunCommandAsync(yaml, new [] {"executable", "--executable", executableStr});
        AssertCommandOutput(tempFile, expectedCommandOutput);
        File.Delete(tempFile);
    }

    private string GetRecursiveCommandsYaml(string file1, string file3)
    {
        return $@"---
            commands:
               - name: recursive
                 parameters:
                    executable: true
                 commands:
                    - name: world1
                      executor: commandir.executors.run
                      parameters:
                         command: echo Hello World1 > {file1}
                    - name: world2
                      commands:
                         - name: world3
                           executor: commandir.executors.run
                           parameters:
                              command: echo Hello World3 > {file3}
        ";
    }

    [Fact]
    public async Task CommandsAreExecutedRecursively()
    {
        string file1 = Path.GetTempFileName(); 
        string file2 = Path.GetTempFileName(); 
        string file3 = Path.GetTempFileName(); 
        string yaml = GetRecursiveCommandsYaml(file1, file3);
        await RunCommandAsync(yaml, new [] {"recursive"});
        AssertCommandOutput(file1, "Hello World1");
        AssertCommandOutput(file3, "Hello World3");
        File.Delete(file1);
        File.Delete(file2);
        File.Delete(file3);
    }


}