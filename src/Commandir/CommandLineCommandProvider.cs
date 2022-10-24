using Commandir.Core;
using Microsoft.Extensions.Logging;
using System.CommandLine;

namespace Commandir;

internal sealed class CommandLineCommandProvider
{
    public Result<CommandLineCommand> FromCommandDefinition(CommandDefinition rootDefinition, ILoggerFactory loggerFactory)
    {
        CommandLineCommand rootCommand = new CommandLineCommand(rootDefinition);
        
        var logger = loggerFactory.CreateLogger<CommandLineCommandProvider>();

        // Add --verbose (logging) option to root command so System.CommandLine will recognize it and ignore it. 
        rootCommand.AddOption(new Option<bool>("--verbose", "Enables verbose logging"));

        foreach(CommandDefinition subCommandData in rootDefinition.Commands)
        {
            Result<CommandLineCommand> result = AddCommand(subCommandData, rootCommand, logger);
            if(result.HasError)
                return result;
        }

        return Result<CommandLineCommand>.Ok(rootCommand);
    }

    private static Result<CommandLineCommand> AddCommand(CommandDefinition commandDefinition, Command parentCommand, ILogger logger)
    {
        if(string.IsNullOrWhiteSpace(commandDefinition.Name))
            return Result<CommandLineCommand>.Error("CommandDefinition `name` is null.");

        CommandLineCommand command = new CommandLineCommand(commandDefinition);
        parentCommand.AddCommand(command);

        foreach(ArgumentDefinition argumentDefinition in commandDefinition.Arguments)
        {
            if(string.IsNullOrWhiteSpace(argumentDefinition.Name))
                return Result<CommandLineCommand>.Error("ArgumentDefinition `name` is null.");

            Argument argument = new Argument<string>(argumentDefinition.Name, argumentDefinition.Description);
            command.AddArgument(argument);
        }

        foreach(OptionDefinition optionDefinition in commandDefinition.Options)
        {
                if(string.IsNullOrWhiteSpace(optionDefinition.Name))
                return Result<CommandLineCommand>.Error("OptionDefinition `name` is null.");

            Option option = new Option<string>($"--{optionDefinition.Name}", optionDefinition.Description) { IsRequired = optionDefinition.Required };
            command.AddOption(option);
        }

        string arguments = string.Join(", ", commandDefinition.Arguments.Select(i => i.Name));
        string options = string.Join(", ", commandDefinition.Options.Select(i => i.Name));
        string commands = string.Join(", ", commandDefinition.Commands.Select(i => i.Name));
        logger.LogDebug("Creating Command: {Name} Arguments: [{Arguments}] Options: [{Options}] Commands: [{Commands}]", commandDefinition.Name, arguments, options, commands);
        foreach(CommandDefinition subCommandDefinition in commandDefinition.Commands)
        {
            Result<CommandLineCommand> result = AddCommand(subCommandDefinition, command, logger);
            if(result.HasError)
                return result;
        }

        return Result<CommandLineCommand>.Ok(command);
    }
}


public static class CommandFactory
{
    internal static Result<CommandLineCommand> FromDefinition(CommandDefinition rootDefinition, ILoggerFactory loggerFactory)
    {
        CommandLineCommand rootCommand = new CommandLineCommand(rootDefinition);
        
        var logger = loggerFactory.CreateLogger("CommandFactory");

        // Add --verbose (logging) option to root command so System.CommandLine will recognize it and ignore it. 
        rootCommand.AddOption(new Option<bool>("--verbose", "Enables verbose logging"));

        foreach(CommandDefinition subCommandData in rootDefinition.Commands)
        {
            Result<CommandLineCommand> result = AddCommand(subCommandData, rootCommand, logger);
            if(result.HasError)
                return result;
        }

        return Result<CommandLineCommand>.Ok(rootCommand);
    }

        private static Result<CommandLineCommand> AddCommand(CommandDefinition commandDefinition, Command parentCommand, ILogger logger)
        {
            if(string.IsNullOrWhiteSpace(commandDefinition.Name))
                return Result<CommandLineCommand>.Error("CommandDefinition `name` is null.");

            CommandLineCommand command = new CommandLineCommand(commandDefinition);
            parentCommand.AddCommand(command);

            foreach(ArgumentDefinition argumentDefinition in commandDefinition.Arguments)
            {
                if(string.IsNullOrWhiteSpace(argumentDefinition.Name))
                    return Result<CommandLineCommand>.Error("ArgumentDefinition `name` is null.");

                Argument argument = new Argument<string>(argumentDefinition.Name, argumentDefinition.Description);
                command.AddArgument(argument);
            }

            foreach(OptionDefinition optionDefinition in commandDefinition.Options)
            {
                 if(string.IsNullOrWhiteSpace(optionDefinition.Name))
                    return Result<CommandLineCommand>.Error("OptionDefinition `name` is null.");

                Option option = new Option<string>($"--{optionDefinition.Name}", optionDefinition.Description) { IsRequired = optionDefinition.Required };
                command.AddOption(option);
            }

            string arguments = string.Join(", ", commandDefinition.Arguments.Select(i => i.Name));
            string options = string.Join(", ", commandDefinition.Options.Select(i => i.Name));
            string commands = string.Join(", ", commandDefinition.Commands.Select(i => i.Name));
            logger.LogDebug("Creating Command: {Name} Arguments: [{Arguments}] Options: [{Options}] Commands: [{Commands}]", commandDefinition.Name, arguments, options, commands);
            foreach(CommandDefinition subCommandDefinition in commandDefinition.Commands)
            {
                Result<CommandLineCommand> result = AddCommand(subCommandDefinition, command, logger);
                if(result.HasError)
                    return result;
            }

            return Result<CommandLineCommand>.Ok(command);
        }

    
}