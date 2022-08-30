using System.IO;
using System.Diagnostics;
using Commandir.Core;

namespace Commandir.Actions
{
    public class RunAction : IAction
    {
        public string Name => "run";

        public async Task ExecuteAsync(ActionExecutionContext context)
        {
            ParameterExecutionContext? shellParameter = context.Parameters.FirstOrDefault(i => i.Name == "shell");
            if(shellParameter == null)
                throw new InvalidOperationException("Failed to find required parameter `shell`");

            string? shell = Convert.ToString(shellParameter.Value);
            if(string.IsNullOrWhiteSpace(shell))
                throw new InvalidOperationException("No value for key `shell`");

            ParameterExecutionContext? runParameter = context.Parameters.FirstOrDefault(i => i.Name == "run");
            if(runParameter == null)
                throw new InvalidOperationException("Failed to find required parameter `run`");

            string? run = Convert.ToString(runParameter.Value);
            if(string.IsNullOrWhiteSpace(run))
                throw new InvalidOperationException("No value for key `run`");

            // replace parameters (if any)
            foreach(ParameterExecutionContext parameter in context.Parameters)
            {
                string parameterName = "${{" + $"{parameter.Name}" + "}}";
                run = run.Replace(parameterName, Convert.ToString(parameter.Value));
            }

            using(var writer = new StreamWriter("runfile.sh"))
            {
                writer.WriteLine(run);
            }

            using(var process = Process.Start(new ProcessStartInfo
            {
                UseShellExecute = false,
                FileName = shell,
                ArgumentList = { "runfile.sh" }
            }))
            if(process != null)
            {
                await process.WaitForExitAsync();
            }
        }
    }
}