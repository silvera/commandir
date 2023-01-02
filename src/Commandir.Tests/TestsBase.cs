using Commandir.Commands;
using Microsoft.Extensions.Logging;
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

    public void AssertContents(string expectedFileContents)
    {
        string fileContents = File
            .ReadAllText(FileName)
            .TrimEnd('\n', '\r', ' ');
        Assert.Equal(expectedFileContents, fileContents);
    }
}

public abstract class TestsBase
{
    protected async Task<CommandExecutionResult?> RunCommandAsync(string yaml, string[] commandLineArgs)
    {
        var rootCommand = new YamlCommandBuilder(yaml).Build();

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var commandExecutor = new CommandExecutor(loggerFactory);

        CommandExecutionResult? result = null;
        var parser = new CommandLineBuilder(rootCommand)
                .AddMiddleware(async (context, next) =>
                {
                    // Do not catch exceptions as we validate they are thrown.
                    result = await commandExecutor.ExecuteAsync(context);
                })
                .Build();

        await parser.InvokeAsync(commandLineArgs);
        return result;
    }
}