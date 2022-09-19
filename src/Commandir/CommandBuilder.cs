using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using Serilog;
using Commandir.Core;

namespace Commandir
{
    public class CommandLineCommand : Command
    {
        public CommandData CommandData { get; set; }

        public CommandLineCommand(CommandData commandData)
            : base(commandData.Name!, commandData.Description)
        {
            CommandData = commandData;
        }
    }

    public class CommandBuilder : IBuilder<CommandLineCommand>
    {
        private readonly CommandData _rootData;
        private readonly Func<IHost, Task> _commandHandler;
        private readonly Microsoft.Extensions.Logging.ILogger _logger;
        public CommandBuilder(CommandData rootData, Func<IHost, Task> commandHandler, ILoggerFactory loggerFactory)
        {
            _rootData = rootData;
            _commandHandler = commandHandler;
            _logger = loggerFactory.CreateLogger<CommandBuilder>();
        }

        public CommandLineCommand Build()
        {
            // Set root Name to avoid exceptions.
            _rootData.Name = "Commandir";
            
            CommandLineCommand rootCommand = new CommandLineCommand(_rootData);
            foreach(CommandData subCommandData in _rootData.Commands)
            {
                AddCommand(subCommandData, rootCommand, _commandHandler);
            }

            return rootCommand;
        }

        private void AddCommand(CommandData commandData, Command parentCommand, Func<IHost, Task> commandHandler)
        {
            if(string.IsNullOrWhiteSpace(commandData.Name))
                throw new ArgumentNullException(nameof(CommandData.Name));

            CommandLineCommand command = new CommandLineCommand(commandData);
            parentCommand.AddCommand(command);

            // Only assign a CommandHandler to leaf commands (or subcommands will not work).
            if(commandData.Commands.Count == 0)
                command.Handler = CommandHandler.Create<IHost>(commandHandler);

            foreach(ArgumentData argumentData in commandData.Arguments)
            {
                if(string.IsNullOrWhiteSpace(commandData.Name))
                    throw new ArgumentNullException(nameof(ArgumentData.Name));

                Argument argument = new Argument<string>(argumentData.Name, argumentData.Description);
                command.AddArgument(argument);
            }

            foreach(OptionData optionData in commandData.Options)
            {
                 if(string.IsNullOrWhiteSpace(optionData.Name))
                    throw new ArgumentNullException(nameof(OptionData.Name));

                Option option = new Option<string>($"--{optionData.Name}", optionData.Description) { IsRequired = optionData.Required };
                command.AddOption(option);
            }

            string arguments = string.Join(",", commandData.Arguments.Select(i => i.Name));
            string options = string.Join(",", commandData.Options.Select(i => i.Name));
            _logger.LogInformation("Loading Definition: {Name} Arguments: [{Arguments}] Options: [{Options}]", commandData.Name, arguments, options);
            foreach(CommandData subCommandData in commandData.Commands)
            {
                AddCommand(subCommandData, command, commandHandler);
            }
        }       
    }
}