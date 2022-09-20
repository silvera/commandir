using Commandir.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;

namespace Commandir
{
    public class CommandLineCommand : Command
    {
        public CommandDefinition CommandDefinition { get; set; }

        public CommandLineCommand(CommandDefinition commandDefinition)
            : base(commandDefinition.Name!, commandDefinition.Description)
        {
            CommandDefinition = commandDefinition;
        }
    }

    public class CommandBuilder
    {
        private readonly CommandDefinition _rootDefinition;
        private readonly Func<IHost, Task> _commandHandler;
        private readonly Microsoft.Extensions.Logging.ILogger _logger;
        public CommandBuilder(ILoggerFactory loggerFactory, CommandDefinition rootDefinition, Func<IHost, Task> commandHandler)
        {
            _logger = loggerFactory.CreateLogger<CommandBuilder>();
            _rootDefinition = rootDefinition;
            _commandHandler = commandHandler;
        }

        public CommandLineCommand Build()
        {   
            CommandLineCommand rootCommand = new CommandLineCommand(_rootDefinition);
            foreach(CommandDefinition subCommandData in _rootDefinition.Commands)
            {
                AddCommand(subCommandData, rootCommand, _commandHandler);
            }

            return rootCommand;
        }

        private void AddCommand(CommandDefinition commandDefinition, Command parentCommand, Func<IHost, Task> commandHandler)
        {
            if(string.IsNullOrWhiteSpace(commandDefinition.Name))
                throw new ArgumentNullException(nameof(CommandDefinition.Name));

            CommandLineCommand command = new CommandLineCommand(commandDefinition);
            parentCommand.AddCommand(command);

            // Only assign a CommandHandler to leaf commands (to support subcommands).
            if(commandDefinition.Commands.Count == 0)
                command.Handler = CommandHandler.Create<IHost>(commandHandler);

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
            _logger.LogInformation("Creating Command: {Name} Arguments: [{Arguments}] Options: [{Options}] [{Commands}]", commandDefinition.Name, arguments, options, commands);
            foreach(CommandDefinition subCommandDefinition in commandDefinition.Commands)
            {
                AddCommand(subCommandDefinition, command, commandHandler);
            }
        }       
    }
}