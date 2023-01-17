using Commandir.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Commandir.Tests;

public class CommandGroupExecutionTests : TestsBase
{
    private string GetCommands()
    {
        return $@"---
           commands:
              - name: group-tests
                parameters:
                   parallel: true
                   logMessage: true
                   delaySeconds: 5
                commands:
                   - name: serial
                     parameters:
                        parallel: false
                     commands:
                        - name: serial1
                          executor: test
                          parameters:
                             message: serial1
                        - name: serial2
                          executor: test
                          parameters:
                             message: serial2
                        - name: serial3
                          executor: test
                          parameters:
                             message: serial3
                   - name: parallel
                     parameters:
                        parallel: true
                     commands:
                        - name: parallel1
                          executor: test
                          parameters:
                             message: parallel1
                        - name: parallel2
                          executor: test
                          parameters:
                             message: parallel2
                        - name: parallel3
                          executor: test
                          parameters:
                             message: parallel3
        ";
    }

    [Fact]    
    public async Task TotalTimeGovernedBySerialCommands()
    {
        string yaml = GetCommands();
        Task<CommandExecutionResult?> commandTask = RunCommandAsync(yaml, new [] {"group-tests"});
        
        // The total time is governed by the serial tasks: 5s + 5s + 5s = 15s
        // We use a 20s delay as a buffer.
        Task delayTask = Task.Delay(System.TimeSpan.FromSeconds(20));
        var resultTask = await Task.WhenAny(commandTask, delayTask);
        Assert.Equal(commandTask, resultTask);
        
        List<string?> expectedCommandResults = new List<string?>
        {
            "serial1",
            "serial2",
            "serial3",
            "parallel1",
            "parallel2",
            "parallel3"
        };
        
        List<string?> commandResults = commandTask.Result!.Results.Select(i => Convert.ToString(i)).ToList();
        Assert.Equal(expectedCommandResults, commandResults);
        
        
        //Assert.Equal(0, commandTask.Result!.Results.Count());
        
        
        // if(parallel)
        // {
        //     // Command Time: ~10 seconds (10 per task)
        //     // Delay Time 15 seconds
        //     // Result: commandTask
        //     Assert.Equal(commandTask, resultTask);
            
        //     CommandExecutionResult? commandResult = await commandTask;
        //     Assert.NotNull(commandResult);
            
        //     string? command1Result = commandResult!.Results.First() as string;
        //     Assert.NotNull(command1Result);
        //     Assert.Equal("Compiled", command1Result);
            
        //     string? command2Result = commandResult!.Results.Last() as string;
        //     Assert.NotNull(command2Result);
        //     Assert.Equal("Tested", command2Result);
        // }
        // else
        // {
        //     // Command Time: 20 seconds (10 per task)
        //     // Delay Time 15 seconds
        //     // Result: delayTask
        //     Assert.Equal(delayTask, resultTask);

        //     // We cannot validate the command results because the command task is not yet complete at this point. 
        // }
    }
}