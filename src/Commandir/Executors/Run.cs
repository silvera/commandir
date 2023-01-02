using Commandir.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Commandir.Executors;

internal class Runner
{
    private readonly string _runnerName;
    private readonly string _runnerExtension;
    private readonly IReadOnlyList<string> _runnerFlags;
    public Runner(string runnerName, string runnerExtension, IReadOnlyList<string> runnerFlags)
    {
        _runnerName = runnerName;
        _runnerExtension = runnerExtension;
        _runnerFlags = runnerFlags;
    }

    public async Task<int> RunAsync(IExecutionContext executionContext)
    {
        ILogger logger = executionContext.LoggerFactory.CreateLogger<Run>();

        object? commandObj = executionContext.ParameterContext.GetParameterValue("command");
        if(commandObj is null)
            throw new Exception("Failed to find parameter `command`");

        string? command = Convert.ToString(commandObj);
        if(command == null)
            throw new Exception("Failed convert parameter `command` to a string.");

        string formattedCommand = executionContext.ParameterContext.FormatParameters(command);

        // Command needs to be written to a file executed by the specified runner.
        // Create a new file in the current directory.
        string runnerFileName = executionContext.Path
            .Replace("/", "_")
            .TrimStart('_') 
            + _runnerExtension;
        
        string runnerFilePath = Path.Combine(Directory.GetCurrentDirectory(), runnerFileName);
        
        // Write the contents of the command to the file.
        logger.LogDebug("Creating file: {RunnerFile} with contents: {RunnerFileContents}", runnerFileName, formattedCommand);
        
        using (var writer = new StreamWriter(runnerFilePath))
        {
            writer.WriteLine(formattedCommand);
        }    

        try
        {
            var processStartInfo = new ProcessStartInfo
            {   CreateNoWindow = true,
                UseShellExecute = false,
                WorkingDirectory = Directory.GetCurrentDirectory(),
                FileName = _runnerName,
            };

            // Add runner flags (if any)
            foreach(string runnerFlag in _runnerFlags)
            {
                processStartInfo.ArgumentList.Add(runnerFlag);
            }
            
            // Add runner file
            processStartInfo.ArgumentList.Add(runnerFilePath);

            string commandLine = $"{_runnerName} {string.Join(" ", processStartInfo.ArgumentList)}";
            logger.LogDebug("Process Starting: {CommandLine}", commandLine);
            var process = Process.Start(processStartInfo);
            if(process == null)
                throw new Exception($"Failed to create process `{commandLine}`");
    
            await process.WaitForExitAsync(executionContext.CancellationToken);
            int exitCode = process.ExitCode;
            logger.LogDebug("Process Complete. ExitCode: {ExitCode}", exitCode);
            return process.ExitCode;
        }
        catch(Exception e)
        {
            logger.LogError(e, "Exeception while exeucting command: {Command}", command);
            return -1;
        }
        finally
        {
            logger.LogDebug("Deleting file: {RunnerFile}", runnerFilePath);
            File.Delete(runnerFilePath);
        }
    }
}

internal sealed class BashRunner : Runner
{
    public BashRunner()
        : base("bash", ".sh", Array.Empty<string>())
    {
    }
}

internal sealed class CmdRunner : Runner
{
    public CmdRunner()
        : base("cmd.exe", ".cmd", new string[]{ "/c" })
    {
    }
}

internal sealed class PowershellRunner : Runner
{
    public PowershellRunner()
        : base("pwsh", ".ps1", Array.Empty<string>())
        {
        }
}


/// <summary>
/// Runs the command specified by the 'command' parameter.
/// </summary>
internal sealed class Run : IExecutor
{
    public async Task<object?> ExecuteAsync(IExecutionContext context)
    {
        Runner? runner = null;
        object? runnerObj = context.ParameterContext.GetParameterValue("runner");
        if(runnerObj is null)
        {
            // Use default runner based on the operating system.
            runner = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? new PowershellRunner()
                : new BashRunner();
        }
        else
        {
            // Use specified runner
            string? runnerStr = runnerObj as string;
            if(runnerStr is null)
                throw new Exception($"Failed to convert runner: {runnerObj} to a string");

            // TODO: Check for runnerFlags and runnerExtensions parameters
            
            runner = runnerStr switch
            {
                "bash" => new BashRunner(),
                "cmd" or "cmd.exe" => new CmdRunner(),
                "pwsh" or "pwsh.exe" => new PowershellRunner(),
                _ => new Runner(runnerStr, ".sh", Array.Empty<string>())
            };
        }

        return await runner.RunAsync(context);
    }
}
