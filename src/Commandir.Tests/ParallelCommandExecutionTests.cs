using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Xunit;

namespace Commandir.Tests;

public class ParallelCommandExecutionTests : TestsBase
{
    private string GetCommands(bool parallel, string sleepCommand, string file1, string file2)
    {
        return $@"---
            commands:
               - name: parallel-tests
                 parameters:
                    executable: true
                    parallel: {parallel}
                 commands:
                    - name: compile
                      parameters:
                         command: |
                            {sleepCommand}
                            echo Compiled > {file1}
                    - name: test
                      parameters:
                         command: |
                            {sleepCommand}
                            echo Tested > {file2}
        ";
    }

    [Theory]
    [InlineData(false, 15, "Compiled", "")]
    [InlineData(true, 15, "Compiled", "Tested")] // Ignore the fact that it makes no sense to test before compiling...
    public async Task ParallelTest(bool parallel, int delaySeconds, string file1Output, string file2Output)
    {
        using var file1 = new TempFile();
        using var file2 = new TempFile();
        string sleepCommand = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? "timeout /t 10"
        : "sleep 10;";
        string yaml = GetCommands (parallel: parallel, sleepCommand, file1.FileName, file2.FileName);
        var runTask = RunCommandAsync(yaml, new [] {"parallel-tests"});
        await Task.WhenAny(runTask, Task.Delay(System.TimeSpan.FromSeconds(delaySeconds)));
        file1.AssertContents(file1Output);
        file2.AssertContents(file2Output);
    }
}