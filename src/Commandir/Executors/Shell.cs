using Commandir.Interfaces;
using System.Diagnostics;
using System.Runtime.InteropServices;

/// <summary>
/// Runs the command specified by the 'run' parameter.
/// </summary>
internal sealed class Shell : IExecutor
{
    private static readonly ShellData BashData =  new ShellData { Name ="bash", Extension = ".sh" };
    private static readonly ShellData CommandData =  new ShellData { Name ="cmd", Extension = ".cmd", Flags = new string[] { "/c" }};
    private static readonly ShellData PowerShellData = new ShellData { Name ="pwsh", Extension = ".ps1" };
    
    public async Task<object?> ExecuteAsync(IExecutionContext context)
    {
        object? shellObj = context.ParameterContext.GetParameterValue("shell");
        string? shellName = shellObj as string;
        if(shellName is null)
        {
            // Choose the default shell (name) based on the OS
            shellName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
            ? "pwsh" 
            : "bash";  
        }

        ShellData shellData = shellName switch
        {
            "bash" => new ShellData 
                { 
                    Name ="bash", 
                    Extension = ".sh" 
                },
            "cmd" or "cmd.exe" => new ShellData 
                { 
                    Name ="cmd", 
                    Extension = ".cmd", 
                    Flags = new string[] { "/c" }
                },
            "pwsh" or "pwsh.exe" => new ShellData 
                { 
                    Name ="pwsh", 
                    Extension = ".ps1" 
                },
            _ => new ShellData 
                { 
                    Name = shellName, 
                    Extension = ".sh" 
                }
        };
        
        object? runObj = context.ParameterContext.GetParameterValue("run");
        if(runObj is null)
            throw new Exception("Failed to find parameter `run`");

        string? run = Convert.ToString(runObj);
        if(run is null)
            throw new Exception("Failed convert parameter `run` to a string.");

        string formattedCommand = context.ParameterContext.FormatParameters(run);

        // The command needs to be written to a file executed by the specified shell.
        string shellFileName = context.Path
            .Replace("/", "_")
            .TrimStart('_') 
            + shellData.Extension;
        
        // Create a new file in the current directory.
        string shellFilePath = Path.Combine(Directory.GetCurrentDirectory(), shellFileName);
        
        // Write the contents of the command to the file.
        context.Logger.Debug("Creating file: {ShellFile} with contents: {ShellFileContents}", shellFilePath, formattedCommand);
        
        using (var writer = new StreamWriter(shellFilePath))
        {
            writer.WriteLine(formattedCommand);
        }    

        try
        {
            var processStartInfo = new ProcessStartInfo
            {   CreateNoWindow = true,
                UseShellExecute = false,
                WorkingDirectory = Directory.GetCurrentDirectory(),
                FileName = shellData.Name,
            };

            // Add shell flags (if any)
            foreach(string runnerFlag in shellData.Flags)
            {
                processStartInfo.ArgumentList.Add(runnerFlag);
            }
            
            // Add shell file
            processStartInfo.ArgumentList.Add(shellFilePath);

            string commandLine = $"{shellData.Name} {string.Join(" ", processStartInfo.ArgumentList)}";
            context.Logger.Debug("Process Starting: {CommandLine}", commandLine);
            var process = Process.Start(processStartInfo);
            if(process == null)
                throw new Exception($"Failed to create process `{commandLine}`");
    
            await process.WaitForExitAsync(context.CancellationToken);
            int exitCode = process.ExitCode;
            context.Logger.Debug("Process Complete. ExitCode: {ExitCode}", exitCode);
            return process.ExitCode;
        }
        catch(Exception e)
        {
            context.Logger.Error(e, "Exeception while exeucting: {Run}", formattedCommand);
            return -1;
        }
        finally
        {
            context.Logger.Debug("Deleting file: {ShellFile}", shellFilePath);
            File.Delete(shellFilePath);
        }
    }
}

internal sealed class ShellData
{
    public string? Name { get; set; }
    public string? Extension { get; set; }
    public IReadOnlyList<string> Flags { get; set; } = Array.Empty<string>();
}