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
 
        var parameterProvider = services.GetRequiredService<IParameterProvider>();

        object? commandObj = parameterProvider.GetParameter("command");
        if(commandObj == null)
           throw new Exception("Failed to find parameter `command`");

        string? command = Convert.ToString(commandObj);
        if(command == null)
            throw new Exception("Failed convert parameter `command` to a string.");

        string shell = "bash";

        var templateFormatter = services.GetRequiredService<ITemplateFormatter2>();
        string formattedCommand = templateFormatter.Format(command, parameterProvider.GetParameters());

        // Create a new file in the current directory.
        Guid guid = Guid.NewGuid();
        string tempFileName = $"commandir_run_{guid}";
        string tempFilePath = Path.Combine(Directory.GetCurrentDirectory(), tempFileName);
        
        // Write the contents of the command to the file.
        logger.LogInformation("Creating file: {TempFile}", tempFilePath);
        using (var writer = new StreamWriter(tempFilePath))
        {
            writer.WriteLine(formattedCommand);
        }

        try
        {
            var process = Process.Start(new ProcessStartInfo
            {
                UseShellExecute = false,
                FileName = shell,
                ArgumentList = { tempFilePath }
            });

            if(process == null)
                throw new Exception($"Failed to create process: {shell} with arguments: {tempFilePath}");
    
            await process.WaitForExitAsync(cancellationToken);
            return process.ExitCode;
        }
        finally
        {
            logger.LogInformation("Deleting file: {TempFile}", tempFilePath);
            File.Delete(tempFilePath);
        }
    }
}
