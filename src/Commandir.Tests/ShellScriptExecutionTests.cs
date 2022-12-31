using Commandir.Commands;
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
                 parameters:
                    executable: true
                 commands:
                    - name: bash
                      parameters:
                         command: ./hello_world.sh > {fileName}
                    - name: pwsh
                      parameters:
                         runner: pwsh
                         command: ./hello_world.ps1 > {fileName}
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
        ICommandExecutionResult result = await RunCommandAsync(yaml, new [] {"shell-script-tests", commandName});
        var failure = result as FailedCommandExecution;
        if(failure is not null)
        {
            System.Console.WriteLine($"Error={failure!.Error}");
        }

        SuccessfulCommandExecution? success = result as SuccessfulCommandExecution;
        Assert.NotNull(success);
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