namespace Commandir;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Reflection;

using Commandir.Core;

public class CommandExecutor
{
    private readonly ILogger _logger;
    
    public CommandExecutor(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<CommandExecutor>();
    }

    public async Task ExecuteAsync(IHost host)
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

        _logger.LogInformation("Executing Command: Name: {Name} Type: {Type}", commandData.Name, commandData.Type);

        Dictionary<string, object?> parameters = new Dictionary<string, object?>();

        // Add command parameters.
        foreach(var parameterPair in command.CommandData.Parameters)
        {
            string name = parameterPair.Key;
            object? value = parameterPair.Value;
            parameters[name] = value;
            _logger.LogInformation("Adding Parameter: Name: {Name} Value: {Value}", name, value);
        }

        // Add command line parameters after command parameters so they can override the parameters.
        foreach(Argument argument in command.Arguments)
        {
            object? value = invocationContext.ParseResult.GetValueForArgument(argument);
            if(value != null)
            {
                string name = argument.Name; 
                parameters[name] = value;
                _logger.LogInformation("Adding Argument: Name: {Name} Value: {Value}", name, value);
            }
        }

        foreach(Option option in command.Options)
        {
            object? value = invocationContext.ParseResult.GetValueForOption(option);
            if(value != null)
            {
                string name = option.Name;
                parameters[name] = value;
                _logger.LogInformation("Adding Option: Name: {Name} Value: {Value}", name, value);
            }  
        }

        ICommandContext commandContext = new CommandContext(host.Services, parameters);
        
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

    private Dictionary<string, Type> GetCommandTypes(Assembly assembly)
    {
        Dictionary<string, Type> types = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        foreach (Type type in assembly.GetTypes())
        {
            if (typeof(ICommand).IsAssignableFrom(type))
            {
                string typeName = type.FullName!;

                types.Add(typeName, type);
                _logger.LogInformation("Loading Command: {Type}", typeName);
            }
        }
        return types;
    }
}