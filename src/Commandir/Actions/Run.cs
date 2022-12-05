using Commandir.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Commandir.Actions;

public sealed class Run : IAction
{
    public async Task<object?> ExecuteAsync(IServiceProvider services)
    {
        var loggerFactory = services.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<Run>();

        var cancellationTokenProvider = services.GetRequiredService<ICancellationTokenProvider>();
        var cancellationToken = cancellationTokenProvider.GetCancellationToken();

        var dynamicCommandProvider = services.GetRequiredService<IDynamicCommandDataProvider>();
        var dynamicCommandData = dynamicCommandProvider.GetCommandData();

        var parameterProvider = services.GetRequiredService<IParameterProvider>();

        object? commandObj = parameterProvider.GetParameter("command");
        if(commandObj == null)
           throw new Exception("Failed to find parameter `command`");

        string? command = Convert.ToString(commandObj);
        if(command == null)
            throw new Exception("Failed convert parameter `command` to a string.");

        string shell = "bash";

        string formattedCommand = parameterProvider.InterpolateParameters(command);

        // Create a new file in the current directory.
        string path = dynamicCommandData!.Path.Replace("/", "_");
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
    
            await process.WaitForExitAsync(cancellationToken!.Value);
            return process.ExitCode;
        }
        finally
        {
            logger.LogInformation("Deleting file: {ScriptFile}", scriptFilePath);
            File.Delete(scriptFilePath);
        }
    }
}
