using System.Diagnostics;

namespace Commandir.New
{
    public abstract class Action
    {
        protected IActionContextProvider Context { get; }
        protected Action(IActionContextProvider context)
        {
            Context = context;
        }

        public abstract Task ExecuteAsync(CancellationToken cancellationToken = default);
    }

    public class RunAction : Action
    {
        public RunAction(IActionContextProvider context, IServiceProvider serviceProvider) : base(context)
        {
        }

        public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            if(!Context.GetParameters().TryGetValue("shell", out object? shellObj))
                throw new Exception();

            string? shell = Convert.ToString(shellObj);
            if(string.IsNullOrWhiteSpace(shell))
                throw new Exception();

            if(!Context.GetParameters().TryGetValue("run", out object? runObj))
                throw new Exception();

            string? run = Convert.ToString(runObj);
            if(string.IsNullOrWhiteSpace(run))
                throw new Exception();

            // replace parameters (if any)
            foreach(var parameterPair in Context.GetParameters())
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