using Commandir.Commands;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Commandir.Tests;

public class ParallelCommandExecutionTests : TestsBase
{
    private string GetCommands(bool parallel)
    {
        return $@"---
            commands:
               - name: parallel-tests
                 parameters:
                    executable: true
                    parallel: {parallel}
                 commands:
                    - name: compile
                      executor: test
                      parameters:
                         message: Compiled
                         delaySeconds: 10
                    - name: test
                      executor: test
                      parameters:
                         message: Tested
                         delaySeconds: 10
        ";
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)] // Ignore the fact that it makes no sense to test before compiling...
    public async Task ParallelTest(bool parallel)
    {
        string yaml = GetCommands(parallel: parallel);
        Task<ICommandExecutionResult> commandTask = RunCommandAsync(yaml, new [] {"parallel-tests"});
        Task delayTask = Task.Delay(System.TimeSpan.FromSeconds(15));
        var resultTask = await Task.WhenAny(commandTask, delayTask);
        if(parallel)
        {
            // Command Time: ~10 seconds (10 per task)
            // Delay Time 15 seconds
            // Result: commandTask
            Assert.Equal(commandTask, resultTask);
            
            SuccessfulCommandExecution? commandResult = await commandTask as SuccessfulCommandExecution;
            Assert.NotNull(commandResult);
            string? command1Result = commandResult!.Results.First() as string;
            Assert.NotNull(command1Result);
            Assert.Equal("Compiled", command1Result);
            string? command2Result = commandResult!.Results.Last() as string;
            Assert.NotNull(command2Result);
            Assert.Equal("Tested", command2Result);
        }
        else
        {
            // Command Time: 20 seconds (10 per task)
            // Delay Time 15 seconds
            // Result: delayTask
            Assert.Equal(delayTask, resultTask);

            // We cannot validate the command results because the command task is not yet complete at this point. 
        }
    }
}