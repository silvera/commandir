using Commandir.Core;
using Microsoft.Extensions.Logging;
using System.CommandLine;

namespace Commandir
{
    internal sealed class CommandLineCommand : Command
    {
        public CommandDefinition CommandDefinition { get; set; }

        public CommandLineCommand(CommandDefinition commandDefinition)
            : base(commandDefinition.Name!, commandDefinition.Description)
        {
            CommandDefinition = commandDefinition;
        }
    }

    internal sealed class CommandBuilder
    {
        private readonly CommandDefinition _rootDefinition;
        private readonly Microsoft.Extensions.Logging.ILogger _logger;
        public CommandBuilder(ILoggerFactory loggerFactory, CommandDefinition rootDefinition)
        {
            _logger = loggerFactory.CreateLogger<CommandBuilder>();
            _rootDefinition = rootDefinition;
        }

        public CommandLineCommand Build()
        {   
            CommandLineCommand rootCommand = new CommandLineCommand(_rootDefinition);
            
            // Add --verbose (logging) option to root command so System.CommandLine will recognize it and ignore it. 
            rootCommand.AddOption(new Option<bool>("--verbose", "Enables verbose logging"));

            foreach(CommandDefinition subCommandData in _rootDefinition.Commands)
            {
                AddCommand(subCommandData, rootCommand);
            }

            return rootCommand;
        }

        private void AddCommand(CommandDefinition commandDefinition, Command parentCommand)
        {
            if(string.IsNullOrWhiteSpace(commandDefinition.Name))
                throw new ArgumentNullException(nameof(CommandDefinition.Name));

            CommandLineCommand command = new CommandLineCommand(commandDefinition);
            parentCommand.AddCommand(command);

            foreach(ArgumentDefinition argumentDefinition in commandDefinition.Arguments)
            {
                if(string.IsNullOrWhiteSpace(argumentDefinition.Name))
                    throw new ArgumentNullException(nameof(ArgumentDefinition.Name));

                Argument argument = new Argument<string>(argumentDefinition.Name, argumentDefinition.Description);
                command.AddArgument(argument);
            }

            foreach(OptionDefinition optionDefinition in commandDefinition.Options)
            {
                 if(string.IsNullOrWhiteSpace(optionDefinition.Name))
                    throw new ArgumentNullException(nameof(OptionDefinition.Name));

                Option option = new Option<string>($"--{optionDefinition.Name}", optionDefinition.Description) { IsRequired = optionDefinition.Required };
                command.AddOption(option);
            }

            string arguments = string.Join(", ", commandDefinition.Arguments.Select(i => i.Name));
            string options = string.Join(", ", commandDefinition.Options.Select(i => i.Name));
            string commands = string.Join(", ", commandDefinition.Commands.Select(i => i.Name));
            _logger.LogDebug("Creating Command: {Name} Arguments: [{Arguments}] Options: [{Options}] Commands: [{Commands}]", commandDefinition.Name, arguments, options, commands);
            foreach(CommandDefinition subCommandDefinition in commandDefinition.Commands)
            {
                AddCommand(subCommandDefinition, command);
            }
        }       
    }
}