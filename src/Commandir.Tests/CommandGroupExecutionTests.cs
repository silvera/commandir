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
                   executable: true
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
        
        // The total time is governed by the serial commands: 5s + 5s + 5s = 15s
        // We use a 20s delay as a buffer.
        Task delayTask = Task.Delay(System.TimeSpan.FromSeconds(20));
        
        var resultTask = await Task.WhenAny(commandTask, delayTask);
        
        // Ensure the commands finish running before the timeout. 
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
    }
}