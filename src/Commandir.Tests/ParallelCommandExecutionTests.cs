using System.Threading.Tasks;
using Xunit;

namespace Commandir.Tests;

public class ParallelCommandExecutionTests : TestsBase
{
    private string GetCommands(bool parallel, string file1, string file2)
    {
        return $@"---
            commands:
               - name: parallel-commands
                 parameters:
                    executable: true
                    parallel: {parallel}
                 commands:
                    - name: compile
                      executor: commandir.executors.run
                      parameters:
                         command: |
                            sleep 10;
                            echo Compiled > {file1}
                    - name: test
                      executor: commandir.executors.run
                      parameters:
                         command: |
                            sleep 10;
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
        string yaml = GetCommands (parallel: parallel, file1.FileName, file2.FileName);
        var runTask = RunCommandAsync(yaml, new [] {"parallel-commands"});
        await Task.WhenAny(runTask, Task.Delay(System.TimeSpan.FromSeconds(delaySeconds)));
        Assert.True(file1.ContentEqual(file1Output));
        Assert.True(file2.ContentEqual(file2Output));
    }
}