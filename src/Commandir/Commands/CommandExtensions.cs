using Commandir.Interfaces;
using Commandir.Services;

using Microsoft.Extensions.DependencyInjection;
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
}

internal static class ServiceExtensions
{
    public static IServiceCollection AddCommandirServices(this IServiceCollection services)
    {
        services.AddSingleton<IActionHandlerProvider, ActionHandlerProvider>();
        services.AddSingleton<IParameterProvider, ParameterProvider>();
        services.AddSingleton<ITemplateFormatter2, StubbleTemplateFormatter2>();
        return services;
    }     
}
