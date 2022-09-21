using Commandir.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Commandir.Builtins;

public class Shell : ICommand
{
    public async Task<CommandResult> ExecuteAsync(CommandContext context)
    {
        var loggerFactory = context.Services.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<Shell>();

        if(!context.Parameters.TryGetValue("command", out object? commandObj))
            throw new Exception($"Failed to find parameter `command`.");
        
        string? command = Convert.ToString(commandObj);
        if(command == null)
            throw new Exception("Failed convert parameter `command` to a string.");

        string shell = "bash";

        var templateFormatter = context.Services.GetRequiredService<ITemplateFormatter>();
        if(templateFormatter == null)
            throw new Exception("Failed to get TemplateFormatter.");

        string formattedCommand = templateFormatter.Format(command, context.Parameters);

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

        await process.WaitForExitAsync(context.CancellationToken);

        logger.LogInformation("Deleting file: {TempFile}", tempFile);
        File.Delete(tempFile);

        return new CommandResult(context, process.ExitCode, formattedCommand);
    }
}