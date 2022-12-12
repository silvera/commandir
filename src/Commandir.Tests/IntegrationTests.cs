using Commandir.Commands;
using Commandir.Yaml;
using Microsoft.Extensions.Logging.Abstractions;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Commandir.Tests;

public class IntegrationTests
{
    private async Task<ICommandExecutionResult> RunCommandAsync(string yaml, string[] commandLineArgs)
    {
        var commandDataProvider = new YamlCommandDataProvider(yaml);
        var rootCommandData = commandDataProvider.GetRootCommandData();
        var rootCommand = YamlCommandBuilder.Build(rootCommandData!);

        var loggerFactory = new NullLoggerFactory();
        var commandExecutor = new CommandExecutor(loggerFactory, commandDataProvider);

        ICommandExecutionResult result = null;
        var parser = new CommandLineBuilder(rootCommand)
                .AddMiddleware(async (context, next) =>
                {
                    result = await commandExecutor!.ExecuteAsync(context);
                    if(result is EmptyCommandExecutionResult)
                    {
                        await next(context);
                    }
                })
                .Build();

        await parser.InvokeAsync(commandLineArgs);
        return result;
    }

    private static void AssertCommandOutput(string fileName, string expectedOutput)
    {
        string fileContents = File.ReadAllText(fileName).TrimEnd('\n');
        Assert.Equal(expectedOutput, fileContents);
    }

    private string GetYaml(string tempFile)
    {
        return $@"---
            commands:
               - name: greet
                 executor: commandir.executors.run
                 parameters:
                    greeting: Hello
                    command: echo {{{{greeting}}}} {{{{name}}}} > {tempFile}
                 arguments:
                    - name: name
                      description: The user's name
                 options:
                    -  name: greeting
                       description: The greeting
                       required: false
               - name: hello
                 parameters:
                         command: echo Hello World > {tempFile}
                 commands:
                    - name: world
                      executor: commandir.executors.run
        ";
    }

    [Fact]
    public async Task ArgumentTest()
    {
        string tempFile = Path.GetTempFileName(); 
        string yaml = GetYaml(tempFile);
        var result = await RunCommandAsync(yaml, new [] {"greet", "World"});
        Assert.True(result.HasResult);
        AssertCommandOutput(tempFile, "Hello World");
        File.Delete(tempFile);
    }

    [Fact]
    public async Task OptionTest()
    {
        string tempFile = Path.GetTempFileName(); 
        string yaml = GetYaml(tempFile);
        var result = await RunCommandAsync(yaml, new [] {"greet", "World", "--greeting", "Hey"});
        Assert.True(result.HasResult);
        AssertCommandOutput(tempFile, "Hey World");
        File.Delete(tempFile);
    }

    [Fact]
    public async Task SubCommandTest()
    {
        string tempFile = Path.GetTempFileName(); 
        string yaml = GetYaml(tempFile);
        var result = await RunCommandAsync(yaml, new [] {"hello", "world"});
        Assert.True(result.HasResult);
        AssertCommandOutput(tempFile, "Hello World");
        File.Delete(tempFile);
    }

    [Theory]
    [InlineData(true, true, "Hello World")]
    [InlineData(false, false, "")]
    public async Task RecurseParameterTest(bool recurse, bool expectedHasResult, string expectedCommandOutput)
    {
        string tempFile = Path.GetTempFileName(); 
        string yaml = GetRecurseTestsYaml(tempFile, recurse: recurse);
        var result = await RunCommandAsync(yaml, new [] {"hello"});
        Assert.Equal(expectedHasResult, result.HasResult);
        AssertCommandOutput(tempFile, expectedCommandOutput);
        File.Delete(tempFile);
    }

    [Theory]
    [InlineData(true, true, "Hello World")]
    [InlineData(false, false, "")]
    public async Task RecurseOptionTest(bool recurse, bool expectedHasResult, string expectedCommandOutput)
    {
        string tempFile = Path.GetTempFileName(); 
        string yaml = GetRecurseTestsYaml(tempFile, recurse: true);
        string recurseStr = $"{recurse}";
        var result = await RunCommandAsync(yaml, new [] {"hello", "--recurse", recurseStr});
        Assert.Equal(expectedHasResult, result.HasResult);
        AssertCommandOutput(tempFile, expectedCommandOutput);
        File.Delete(tempFile);
    }

    private string GetRecurseTestsYaml(string tempFile, bool recurse)
    {
        return $@"---
            commands:
               - name: hello
                 parameters:
                    recurse: {recurse}
                 options:
                    - name: recurse
                      description: Determines whether or not to execute child commands
                 commands:
                    - name: world
                      executor: commandir.executors.run
                      parameters:
                         command: echo Hello World > {tempFile}

        ";
    }

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
                         command: echo Hello World1 > {file1}
                    - name: world2
                      executor: commandir.executors.run
                      parameters:
                         command: echo Hello World2 > {file2}

        ";
    }

    [Theory]
    [InlineData(false, "Hello World1", "Hello World2")]
    [InlineData(true, "Hello World1", "Hello World2")]
    public async Task ParallelParameterTest(bool parallel, string file1Output, string file2Output)
    {
        string file1 = Path.GetTempFileName(); 
        string file2 = Path.GetTempFileName(); 
        string yaml = GetParallelTestsYaml(file1, file2, parallel: parallel);
        await RunCommandAsync(yaml, new [] {"hello"});
        AssertCommandOutput(file1, file1Output);
        AssertCommandOutput(file2, file2Output);
        File.Delete(file1);
        File.Delete(file2);
    }

    [Theory]
    [InlineData(false, "Hello World1", "Hello World2")]
    [InlineData(true, "Hello World1", "Hello World2")]
    public async Task ParallelOptionTest(bool parallel, string file1Output, string file2Output)
    {
        string file1 = Path.GetTempFileName(); 
        string file2 = Path.GetTempFileName(); 
        string yaml = GetParallelTestsYaml(file1, file2, parallel: true);
        string parallelStr = $"{parallel}";
        await RunCommandAsync(yaml, new [] {"hello", "--parallel", parallelStr});
        AssertCommandOutput(file1, file1Output);
        AssertCommandOutput(file2, file2Output);
        File.Delete(file1);
        File.Delete(file2);
    }
}