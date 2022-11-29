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
            throw new Exception ("Failed convert parameter `command` to a string.");

        string shell = "bash";

        var templateFormatter = request.Services.GetRequiredService<ITemplateFormatter2>();
        string formattedCommand = templateFormatter.Format(command, parameterProvider.GetParameters());

        // Create a temporary file.
        string tempFile = Path.GetTempFileName();
        
        // Write the contents of the command to the file.
        using (var writer = new StreamWriter(tempFile))
        {
            writer.WriteLine(formattedCommand);
            logger.LogInformation("Wrote command: {Command} to file: {TempFile}", formattedCommand, tempFile);
        }

        logger.LogInformation("Executing command: {Command}", formattedCommand);

        using var process = Process.Start(new ProcessStartInfo
        {
            UseShellExecute = false,
            FileName = shell,
            ArgumentList = { tempFile }
        });
        if(process == null)
            throw new Exception($"Failed to create process: {shell} with arguments: {tempFile}");

        await process.WaitForExitAsync(request.CancellationToken);

        logger.LogInformation("Deleting file: {TempFile}", tempFile);
        File.Delete(tempFile);

        return new ActionResponse();
    }
}
