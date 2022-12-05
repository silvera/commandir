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
        List<string> names = new List<string> { command.Name };
        command.GetParentNames(names);
        names.Reverse();
        return "/" + string.Join("/", names); 
    }

    private static void GetParentNames(this Command command, List<string> names)
    {
        foreach(var parent in command.Parents)
        {
            if(parent is Command parentCommand)
            {
                names.Add(parentCommand.Name);
                parentCommand.GetParentNames(names);
            }
        }
    }
}
