using System.Diagnostics;

namespace Commandir.New
{
    public abstract class Action
    {
        protected IActionContext Context { get; }
        protected Action(IActionContext context)
        {
            Context = context;
        }

        public abstract Task ExecuteAsync(CancellationToken cancellationToken = default);
    }

    public class RunAction : Action
    {
        public RunAction(ActionContext context) : base(context)
        {
        }

        public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            if(!Context.Parameters.TryGetValue("shell", out object? shellObj))
                throw new Exception();

            string? shell = Convert.ToString(shellObj);
            if(string.IsNullOrWhiteSpace(shell))
                throw new Exception();

            if(!Context.Parameters.TryGetValue("run", out object? runObj))
                throw new Exception();

            string? run = Convert.ToString(runObj);
            if(string.IsNullOrWhiteSpace(run))
                throw new Exception();

            // replace parameters (if any)
            foreach(var parameterPair in Context.Parameters)
            {
                string parameterName = "${{" + $"{parameterPair.Key}" + "}}";
                run = run.Replace(parameterName, Convert.ToString(parameterPair.Value));
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