namespace Commandir;

using Commandir.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;

public class CommandExecutor
{
    private readonly CommandProvider _commandProvider;
    private readonly ILogger _logger;
    
    public CommandExecutor(CommandProvider commandProvider, ILoggerFactory loggerFactory)
    {
         _commandProvider = commandProvider;
        _logger = loggerFactory.CreateLogger<CommandExecutor>();
    }

    private void AddParameter(Dictionary<string, object?> parameters, string parameterType, string parameterName, object? parameterValue)
    {
        bool isOverride = parameters.ContainsKey(parameterName);
        parameters[parameterName] = parameterValue;
        _logger.LogInformation("Adding {ParameterType}: Name: {ParameterName} Value: {ParameterValue} IsOverride: {IsOverride}", parameterType, parameterName, parameterValue, isOverride);
    }


    public async Task ExecuteAsync(IHost host)
    {
        ParseResult parseResult = host.Services.GetRequiredService<InvocationContext>().ParseResult!;
        CommandLineCommand command = (CommandLineCommand)parseResult.CommandResult.Command;
        CommandDefinition commandDefinition = command.CommandDefinition;
        
         string? commandTypeStr  = commandDefinition.Type;
        if(commandTypeStr == null)
            throw new Exception("Command type cannot be null.");
    
        ICommand? commandImpl = _commandProvider.GetCommand(commandTypeStr);
        if(commandImpl == null)
            throw new Exception($"Failed to create an instance of the command `{commandTypeStr}`");

        _logger.LogInformation("Executing Command: Name: {Name} Type: {Type}", commandDefinition.Name, commandDefinition.Type);

        // Create parameters dictionary
        Dictionary<string, object?> parameters = new Dictionary<string, object?>();

        // Add command parameters.
        foreach(var parameterPair in commandDefinition.Parameters)
        {
            AddParameter(parameters, "Parameter", parameterPair.Key, parameterPair.Value);
        }

        // Add command line parameters after command parameters so they can override the parameters.
        foreach(Argument argument in command.Arguments)
        {
            object? value = parseResult.GetValueForArgument(argument);
            if(value != null)
            {
                AddParameter(parameters, "Argument", argument.Name, value);
            }
        }

        foreach(Option option in command.Options)
        {
            object? value = parseResult.GetValueForOption(option);
            if(value != null)
            {
                AddParameter(parameters, "Option", option.Name, value);
            }  
        }

        ICommandContext commandContext = new CommandContext(host.Services, parameters);
        try
        {
            await commandImpl.ExecuteAsync(commandContext);
        }
        catch(Exception e)
        {
            _logger.LogCritical("Failed to execute command: {CommandType} Error: {Error}", commandImpl.GetType().FullName, e.Message);
        }
    }
}