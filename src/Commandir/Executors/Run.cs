using Commandir.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Commandir.Executors;

/// <summary>
/// Runs the command speciifed by the 'command' parameter.
/// </summary>
internal sealed class Run : IExecutor
{
    public async Task<object?> ExecuteAsync(IExecutionContext context)
    {
        ILogger logger = context.LoggerFactory.CreateLogger<Run>();
        logger.LogInformation("Executing command: {CommandPath}", context.Path);

        object? commandObj = context.ParameterContext.GetParameterValue("command");
        if(commandObj is null)
            throw new Exception("Failed to find parameter `command`");

        string? command = Convert.ToString(commandObj);
        if(command == null)
            throw new Exception("Failed convert parameter `command` to a string.");

        string formattedCommand = context.ParameterContext.FormatParameters(command);

        // Determine the current OS and use the default runner for each (if no 'runner' parameter is specified by the user).
        // Linux/MacOS/Others: bash
        // Windows: cmd.exe

        string runner = "bash";
        if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            runner = "cmd.exe";
        }

        logger.LogInformation("Runner: {Runner} Command: {Command}", runner, formattedCommand);

        // Create a new file in the current directory.
        string scriptFileName = context.Path.Replace("/", "_").TrimStart('_') + ".cmd";
        string scriptFilePath = Path.Combine(Directory.GetCurrentDirectory(), scriptFileName);
        
        // Write the contents of the command to the file.
        logger.LogInformation("Creating file: {ScriptPath}", scriptFilePath);
        
        using (var writer = new StreamWriter(scriptFilePath))
        {
            writer.WriteLine(formattedCommand);
        }

        try
        {
            var processStartInfo = new ProcessStartInfo
            {   CreateNoWindow = true,
                UseShellExecute = false,
                WorkingDirectory = Directory.GetCurrentDirectory(),
                FileName = runner,
                ArgumentList = { "/c", scriptFilePath }
            };
            
            string commandLine = $"{runner} {string.Join(" ", processStartInfo.ArgumentList)}";
            logger.LogInformation("Process Starting: {CommandLine}", commandLine);
            var process = Process.Start(processStartInfo);
            if(process == null)
                throw new Exception($"Failed to create process `{commandLine}`");
    
            await process.WaitForExitAsync(context.CancellationToken);
            int exitCode = process.ExitCode;
            logger.LogInformation("Process Complete. ExitCode: {ExitCode}", exitCode);
            return process.ExitCode;
        }
        catch(Exception e)
        {
            logger.LogError(e, "Exeception while exeucting command: {Command}", formattedCommand);
            return -1;
        }
        finally
        {
            logger.LogInformation("Deleting file: {ScriptPath}", scriptFilePath);
            File.Delete(scriptFilePath);
        }
    }
}
