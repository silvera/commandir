using Microsoft.Extensions.Hosting;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;

namespace Commandir.Commands;

internal static class CommandExtensions
{
    internal static void SetHandlers(this Command command, Func<IServiceProvider, Task> invocationHandler, Action<Exception> exceptionHandler)
    {
        if(command.Subcommands.Count == 0)
        {
            command.Handler = CommandHandler.Create<IHost>(async host => 
            {
                try
                {
                    await invocationHandler(host.Services);
                }
                catch(Exception e)
                {
                    exceptionHandler(e);
                }                
            });
        }
        else
        {
            foreach(Command subCommand in command.Subcommands)
            {
                subCommand.SetHandlers(invocationHandler, exceptionHandler);
            }
        }
    }

    internal static string GetPath(this Command command)
    {
        List<string> components = new List<string>();
        command.GetPathComponents(components);
        components.Reverse();
        return string.Concat(components);
    }

    private static void GetPathComponents(this Command command, List<string> names)
    {
        names.Add($"/{command.Name}");
        foreach(var parent in command.Parents)
        {
            if(parent is Command parentCommand)
            {
                parentCommand.GetPathComponents(names);
            }
        }
    }
}
