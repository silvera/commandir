using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;

namespace Commandir.Tests;

public class ShellCommandExecutionTests : TestsBase
{
    private string GetCommands(string fileName)
    {
        return $@"---
            commands:
               - name: shell-command-tests
                 commands:
                    - name: bash
                      parameters:
                         run: echo Hello World > {fileName}
                    - name: cmd
                      parameters:
                         shell: cmd
                         run: echo Hello World > {fileName}
                    - name: pwsh
                      parameters:
                         shell: pwsh
                         run: Write-Output 'Hello World' > {fileName}
        ";
    }

    private static IReadOnlyList<string> GetCommandsPerOS()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? new [] { "cmd", "pwsh" }
        : new [] { "bash" };
    }

    public async Task RunCommand(string commandName)
    { 
        using TempFile file = new TempFile();
        string yaml = GetCommands(file.FileName);
        await RunCommandAsync(yaml, new [] {"shell-command-tests", commandName});
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