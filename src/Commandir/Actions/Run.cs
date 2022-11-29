using Commandir.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Commandir.Actions;

public sealed class Run : IActionHandler
{
    public async Task<ActionResponse> HandleAsync(ActionRequest request)
    {
        var loggerFactory = request.Services.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<Run>();

        var parameterProvider = request.Services.GetRequiredService<IParameterProvider>();

        object? commandObj = parameterProvider.GetParameter("command");
        if(commandObj == null)
           throw new Exception("Failed to find parameter `command`");

        string? command = Convert.ToString(commandObj);
        if(command == null)
            throw new Exception("Failed convert parameter `command` to a string.");

        string shell = "bash";

        var templateFormatter = request.Services.GetRequiredService<ITemplateFormatter2>();
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

        // Create a new response with ExitCode = -1;
        var response = new ActionResponse { Value = -1 };
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
    
            await process.WaitForExitAsync(request.CancellationToken);
            response.Value = process.ExitCode;
        }
        finally
        {
            logger.LogInformation("Deleting file: {TempFile}", tempFilePath);
            File.Delete(tempFilePath);
        }

        return response;
    }
}
