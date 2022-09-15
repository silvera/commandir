namespace Commandir;

using Commandir.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine.Invocation;
using System.Reflection;

public static class CommandExecutor
{
    public static async Task ExecuteAsync(IHost host)
    {
        InvocationContext invocationContext = host.Services.GetRequiredService<InvocationContext>();
        if(invocationContext == null)
            throw new Exception("Failed to obtain InvocationContext from ServiceProvider.");

        CommandLineCommand? command = invocationContext.ParseResult.CommandResult.Command as CommandLineCommand;
        if(command == null)
            throw new Exception("Failed to obtain command from InvocationContext.");

        CommandData commandData = command.CommandData;
        
        string? commandTypeStr  = command.CommandData.Type;
        if(commandTypeStr == null)
            throw new Exception("Command type cannot be null.");
    
        Dictionary<string, Type> commandTypes = GetCommandTypes(typeof(Program).Assembly);        
        if(!commandTypes.TryGetValue(commandTypeStr, out Type? commandType))
            throw new Exception($"Failed to find Command type `{commandTypeStr}`");
        
        ICommand? commandImpl = Activator.CreateInstance(commandType) as ICommand;
        if(commandImpl == null)
            throw new Exception($"Failed to create an instance of the command type `{commandType}`");

        ICommandContext commandContext = new CommandContext(host.Services);
        
        try
        {
            await commandImpl.ExecuteAsync(commandContext);
        }
        catch(Exception e)
        {
            System.Console.WriteLine(e.Message);
            throw;
        }
    }

    private static Dictionary<string, Type> GetCommandTypes(Assembly assembly)
    {
        Dictionary<string, Type> types = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        foreach (Type type in assembly.GetTypes())
        {
            if (typeof(ICommand).IsAssignableFrom(type))
            {
                types.Add(type.FullName!, type);
            }
        }
        return types;
    }
}