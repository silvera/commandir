using Commandir.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Commandir.Executors;

/// <summary>
/// Runs the command speciifed by the 'command' parameter.
/// </summary>
internal sealed class Run : IExecutor
{
    public async Task<object?> ExecuteAsync(IExecutionContext context)
    {
        ILogger logger = context.LoggerFactory.CreateLogger<Run>();

        object? commandObj = context.ParameterContext.GetParameterValue("command");
        if(commandObj is null)
            throw new Exception("Failed to find parameter `command`");

        string? command = Convert.ToString(commandObj);
        if(command == null)
            throw new Exception("Failed convert parameter `command` to a string.");

        string formattedCommand = context.ParameterContext.FormatParameters(command);

        // TODO: Determine the current OS and use the default runner for each (if no 'runner' parameter is specified by the user).
        // Linux/MacOS: bash
        // Windows: cmd.exe

        string runner = "bash";

        // Create a new file in the current directory.
        string scriptFileName = context.Path.Replace("/", "_").TrimStart('_') + ".sh";
        string scriptFilePath = Path.Combine(Directory.GetCurrentDirectory(), scriptFileName);
        
        // Write the contents of the command to the file.
        logger.LogInformation("Creating file: {ScriptPath}", scriptFilePath);
        
        using (var writer = new StreamWriter(scriptFilePath))
        {
            writer.WriteLine(formattedCommand);
        }

        try
        {
            var process = Process.Start(new ProcessStartInfo
            {
                UseShellExecute = false,
                FileName = runner,
                ArgumentList = { scriptFilePath }
            });

            if(process == null)
                throw new Exception($"Failed to create process: {runner} with arguments: {scriptFilePath}");
    
            await process.WaitForExitAsync(context.CancellationToken);
            return process.ExitCode;
        }
        finally
        {
            logger.LogInformation("Deleting file: {ScriptPath}", scriptFilePath);
            File.Delete(scriptFilePath);
        }
    }
}
