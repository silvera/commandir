using Commandir.Commands;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Commandir.Tests;

public class ScriptExecutionTests : TestsBase
{
    private string GetCommands(string file1, string currentDirectory)
    {
        return $@"---
            commands:
               - name: script-tests
                 parameters:
                    executable: true
                 commands:
                    - name: relative-path
                      executor: commandir.executors.run
                      parameters:
                         command: ./hello_world.sh > {file1}
                    - name: absolute-path
                      executor: commandir.executors.run
                      parameters:
                         command: {currentDirectory}/hello_world.sh > {file1}
        ";
    }

    [Fact]
    public async Task RelativePath()
    {
        using var file1 = new TempFile(); 
        string yaml = GetCommands(file1.FileName, Directory.GetCurrentDirectory());
        ICommandExecutionResult result = await RunCommandAsync(yaml, new [] {"script-tests", "relative-path"});
        Assert.True(result is SuccessfulCommandExecution);
        file1.AssertContents("Hello World!");
    }

    [Fact]
    public async Task AbsolutePath()
    {
        using var file1 = new TempFile(); 
        string yaml = GetCommands(file1.FileName, Directory.GetCurrentDirectory());
        ICommandExecutionResult result = await RunCommandAsync(yaml, new [] {"script-tests", "absolute-path"});
        Assert.True(result is SuccessfulCommandExecution);
        file1.AssertContents("Hello World!");
    }
}