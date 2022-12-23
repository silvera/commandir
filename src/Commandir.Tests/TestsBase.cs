using Commandir.Commands;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Commandir.Tests;

internal sealed class TempFile : IDisposable
{
    public string FileName { get; }
    public TempFile()
    {
        FileName = Path.GetTempFileName();
    }

    public void Dispose()
    {
        File.Delete(FileName);
    }

    public string GetContent()
    {
        return File.ReadAllText(FileName).TrimEnd('\n');
    }

    public void AssertContents(string expectedFileContents)
    {
        string fileContents = File.ReadAllText(FileName).TrimEnd('\n');
        Assert.Equal(expectedFileContents, fileContents);
    }

    public bool ContentEqual(string expectedContent)
    {
        string fileContent = File.ReadAllText(FileName).TrimEnd('\n');
        return fileContent == expectedContent;
    }
}

public abstract class TestsBase
{
    protected void AssertCommandOutput(string fileName, string expectedOutput)
    {
        string fileContents = File.ReadAllText(fileName).TrimEnd('\n');
        Assert.Equal(expectedOutput, fileContents);
    }

    protected async Task<ICommandExecutionResult> RunCommandAsync(string yaml, string[] commandLineArgs)
    {
        var rootCommand = new YamlCommandBuilder(yaml).Build();

        var loggerFactory = new NullLoggerFactory();
        var commandExecutor = new CommandExecutor(loggerFactory /*commandDataProvider*/);

        ICommandExecutionResult? result = null;
        var parser = new CommandLineBuilder(rootCommand)
                .AddMiddleware(async (context, next) =>
                {
                    result = await commandExecutor.ExecuteAsync(context);
                    if(result is FailedCommandExecution failure)
                    {
                        await next(context);
                    }
                })
                .Build();

        await parser.InvokeAsync(commandLineArgs);
        return result!;
    }
}