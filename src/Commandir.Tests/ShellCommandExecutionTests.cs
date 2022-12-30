using Commandir.Commands;
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
               - name: command-tests
                 parameters:
                    executable: true
                 commands:
                    - name: bash
                      parameters:
                         command: echo Hello World > {fileName}
                    - name: cmd
                      parameters:
                         runner: cmd
                         command: echo Hello World > {fileName}
                    - name: pwsh
                      parameters:
                         runner: pwsh
                         command: Write-Output 'Hello World' > {fileName}
        ";
    }

    private static IReadOnlyList<string> GetCommandsPerOS()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? new [] { "cmd" }
        : new [] { "bash" };
    }

    [Fact]
    public async Task RunCommands()
    { 
        foreach(string commmandName in GetCommandsPerOS())
        {
            await RunCommand(commmandName);
        }   
    }

    public async Task RunCommand(string commandName)
    { 
        using TempFile file = new TempFile();
        string yaml = GetCommands(file.FileName);
        ICommandExecutionResult result = await RunCommandAsync(yaml, new [] {"command-tests", commandName});
        SuccessfulCommandExecution? success = result as SuccessfulCommandExecution;
        Assert.NotNull(success);
        file.AssertContents("Hello World");
    }
}