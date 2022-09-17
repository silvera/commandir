namespace Commandir;

using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using System.CommandLine.Invocation;

using Commandir.Core;

public sealed class CommandContext : ICommandContext
{
    public IServiceProvider Services { get; }
    
    public IReadOnlyDictionary<string, object?> Parameters { get; } 
    
    public CommandContext(IServiceProvider services, IReadOnlyDictionary<string, object?> parameters)
    {
        Services = services;
        Parameters  = parameters;
    }
}