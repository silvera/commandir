using Microsoft.Extensions.Hosting;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;

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
        public CommandBuilder(CommandData rootData, Func<IHost, Task> commandHandler)
        {
            _rootData = rootData;
            _commandHandler = commandHandler;
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

        private static void AddCommand(CommandData commandData, Command parentCommand, Func<IHost, Task> commandHandler)
        {
            if(string.IsNullOrWhiteSpace(commandData.Name))
                throw new ArgumentNullException(nameof(CommandData.Name));

            CommandLineCommand command = new CommandLineCommand(commandData);
            command.Handler = CommandHandler.Create<IHost>(commandHandler);
            parentCommand.AddCommand(command);

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

            foreach(CommandData subCommandData in commandData.Commands)
            {
                AddCommand(subCommandData, command, commandHandler);
            }
        }       
    }
}