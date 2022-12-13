using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Commandir.Tests;

public class ParallelTests : TestsBase
{
    private string GetParallelTestsYaml(string file1, string file2, bool parallel)
    {
        return $@"---
            commands:
               - name: hello
                 parameters:
                    recurse: true
                    parallel: {parallel}
                 options:
                    - name: parallel
                      description: Determines whether or not to execute child commands in parallel
                 commands:
                    - name: world1
                      executor: commandir.executors.run
                      parameters:
                         command: |
                            sleep 10;
                            echo Hello World1 > {file1}
                    - name: world2
                      executor: commandir.executors.run
                      parameters:
                         command: |
                            sleep 10;
                            echo Hello World2 > {file2}

        ";
    }

    [Theory]
    [InlineData(false, 15, "Hello World1", "")]
    [InlineData(true, 15, "Hello World1", "Hello World2")]
    public async Task ParallelTest(bool parallel, int delaySeconds, string file1Output, string file2Output)
    {
        string file1 = Path.GetTempFileName(); 
        string file2 = Path.GetTempFileName(); 
        string yaml = GetParallelTestsYaml(file1, file2, parallel: parallel);
        var runTask = RunCommandAsync(yaml, new [] {"hello"});
        await Task.WhenAny(runTask, Task.Delay(System.TimeSpan.FromSeconds(delaySeconds)));
        AssertCommandOutput(file1, file1Output);
        AssertCommandOutput(file2, file2Output);
        File.Delete(file1);
        File.Delete(file2);
    }
}