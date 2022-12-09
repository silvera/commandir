
using Commandir.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Commandir.Executors;

public sealed class Run : IExecutor
{
    public async Task<object?> ExecuteAsync(IExecutionContext context)
    {
        var loggerFactory = context.LoggerFactory;
        var logger = loggerFactory.CreateLogger<Run>();

        var cancellationToken = context.CancellationToken; 

        if(!context.ParameterContext.Parameters.TryGetValue("command", out object? commandObj))
            throw new Exception("Failed to find parameter `command`");

        string? command = Convert.ToString(commandObj);
        if(command == null)
            throw new Exception("Failed convert parameter `command` to a string.");

        string shell = "bash";

        string formattedCommand = context.ParameterContext.Format(command);

        // Create a new file in the current directory.
        string path = context.Path.Replace("/", "_");
        string scriptFileName = $"commandir{path}.sh";
        string scriptFilePath = Path.Combine(Directory.GetCurrentDirectory(), scriptFileName);
        
        // Write the contents of the command to the file.
        logger.LogInformation("Creating file: {ScriptFile}", scriptFilePath);
        using (var writer = new StreamWriter(scriptFilePath))
        {
            writer.WriteLine(formattedCommand);
        }

        try
        {
            var process = Process.Start(new ProcessStartInfo
            {
                UseShellExecute = false,
                FileName = shell,
                ArgumentList = { scriptFilePath }
            });

            if(process == null)
                throw new Exception($"Failed to create process: {shell} with arguments: {scriptFilePath}");
    
            await process.WaitForExitAsync(context.CancellationToken);  
            return process.ExitCode;
        }
        finally
        {
            logger.LogInformation("Deleting file: {ScriptFile}", scriptFilePath);
            File.Delete(scriptFilePath);
        }
    }
}
