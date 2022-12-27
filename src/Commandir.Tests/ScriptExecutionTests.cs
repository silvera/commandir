using Commandir.Commands;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Xunit;

namespace Commandir.Tests;

public class ScriptExecutionTests : TestsBase
{
    private string GetLinuxCommands(string currentDirectory, string file1)
    {
        return $@"---
            commands:
               - name: script-tests
                 parameters:
                    executable: true
                 commands:
                    - name: relative-path
                      parameters:
                         command: ./hello_world.sh > {file1}
                    - name: absolute-path
                      parameters:
                         command: {currentDirectory}/hello_world.sh > {file1}
        ";
    }
    private string GetWindowsCommands(string currentDirectory, string file1)
    {
        return $@"---
            commands:
               - name: script-tests
                 parameters:
                    executable: true
                 commands:
                    - name: relative-path
                      parameters:
                         command: CALL hello_world.bat > {file1}
                    - name: absolute-path
                      parameters:
                         command: CALL {currentDirectory}/hello_world.bat > {file1}
        ";
    }

    [Fact]
    public async Task RelativePath()
    {
        using var file1 = new TempFile(); 
        string currentDirectory = Directory.GetCurrentDirectory();
        string yaml = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? GetWindowsCommands(currentDirectory, file1.FileName)
            : GetLinuxCommands(currentDirectory, file1.FileName);
        ICommandExecutionResult result = await RunCommandAsync(yaml, new [] {"script-tests", "relative-path"});
        Assert.True(result is SuccessfulCommandExecution);
        file1.AssertContents("Hello World!");
    }

    [Fact]
    public async Task AbsolutePath()
    {
        using var file1 = new TempFile(); 
        string currentDirectory = Directory.GetCurrentDirectory();
        string yaml = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? GetWindowsCommands(currentDirectory, file1.FileName)
            : GetLinuxCommands(currentDirectory, file1.FileName);
        ICommandExecutionResult result = await RunCommandAsync(yaml, new [] {"script-tests", "absolute-path"});
        Assert.True(result is SuccessfulCommandExecution);
        file1.AssertContents("Hello World!");
    }
}