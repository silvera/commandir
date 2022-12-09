using Microsoft.Extensions.Hosting;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;

namespace Commandir.Commands;

internal static class CommandExtensions
{
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
