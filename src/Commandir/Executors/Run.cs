using Commandir.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Commandir.Executors;

internal abstract class RunnerBase
{
    private readonly string _runner;
    private readonly string _runnerExtension;
    private readonly string _runnerFlags;
    protected RunnerBase(string runner, string runnerExtension, string runnerFlags)
    {
        _runner = runner;
        _runnerExtension = runnerExtension;
        _runnerFlags = runnerFlags;
    }

    public async Task<int> RunAsync(IExecutionContext executionContext)
    {
        ILogger logger = executionContext.LoggerFactory.CreateLogger<Run>();
        logger.LogInformation("Executing command: {CommandPath}", executionContext.Path);

        object? commandObj = executionContext.ParameterContext.GetParameterValue("command");
        if(commandObj is null)
            throw new Exception("Failed to find parameter `command`");

        string? command = Convert.ToString(commandObj);
        if(command == null)
            throw new Exception("Failed convert parameter `command` to a string.");

        string formattedCommand = executionContext.ParameterContext.FormatParameters(command);

        logger.LogInformation("Runner: {Runner} Command: {Command}", _runner, formattedCommand);

        // Command needs to be written to a file executed by the specified runner.
        // Create a new file in the current directory.
        string runnerFileName = executionContext.Path
            .Replace("/", "_")
            .TrimStart('_') 
            + _runnerExtension;
        
        string runnerFilePath = Path.Combine(Directory.GetCurrentDirectory(), runnerFileName);
        
        // Write the contents of the command to the file.
        logger.LogInformation("Creating file: {RunnerFile}", runnerFileName);
        
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
                FileName = _runner,
            };

            // Add runner flags (if any)
            if(!string.IsNullOrWhiteSpace(_runnerFlags))
            {
                processStartInfo.Arguments = _runnerFlags;
            }
            
            // Add script file
            processStartInfo.ArgumentList.Add(runnerFilePath);

            string commandLine = $"{_runner} {string.Join(" ", processStartInfo.ArgumentList)}";
            logger.LogInformation("Process Starting: {CommandLine}", commandLine);
            var process = Process.Start(processStartInfo);
            if(process == null)
                throw new Exception($"Failed to create process `{commandLine}`");
    
            await process.WaitForExitAsync(executionContext.CancellationToken);
            int exitCode = process.ExitCode;
            logger.LogInformation("Process Complete. ExitCode: {ExitCode}", exitCode);
            return process.ExitCode;
        }
        catch(Exception e)
        {
            logger.LogError(e, "Exeception while exeucting command: {Command}", command);
            return -1;
        }
        finally
        {
            logger.LogInformation("Deleting file: {ScriptPath}", runnerFilePath);
            File.Delete(runnerFilePath);
        }
    }
}

internal sealed class BashRunner : RunnerBase
{
    public BashRunner()
        : base("bash", ".sh", string.Empty)
    {
    }
}

internal sealed class CmdRunner : RunnerBase
{
    public CmdRunner()
        : base("cmd.exe", ".cmd", "/c")
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
        // TODO: Look for a user-defined runner, runnerFlags etc.
        // Determine the current OS and use the default runner for each.
        // Linux/MacOS/Others: bash
        // Windows: cmd.exe
        RunnerBase runner = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? new CmdRunner()
        : new BashRunner();

        return await runner.RunAsync(context);
    }
}
