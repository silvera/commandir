using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;

namespace Commandir.Tests;

public class ShellScriptExecutionTests : TestsBase
{
    private string GetCommands(string fileName)
    {
        return $@"---
            commands:
               - name: shell-script-tests
                 commands:
                    - name: bash
                      parameters:
                         run: ./hello_world.sh > {fileName}
                    - name: pwsh
                      parameters:
                         shell: pwsh
                         run: ./hello_world.ps1 > {fileName}
        ";
    }

    private static IReadOnlyList<string> GetCommandsPerOS()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? new [] { "pwsh" }
        : new [] { "bash" };
    }

    public async Task RunCommand(string commandName)
    { 
        using TempFile file = new TempFile();
        string yaml = GetCommands(file.FileName);
        await RunCommandAsync(yaml, new [] {"shell-script-tests", commandName});
        file.AssertContents("Hello World");
    }

    [Fact]
    public async Task RunCommands()
    { 
        foreach(string commmandName in GetCommandsPerOS())
        {
            await RunCommand(commmandName);
        }   
    }
}