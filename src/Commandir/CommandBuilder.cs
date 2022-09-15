using Microsoft.Extensions.Hosting;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;

namespace Commandir
{
    public class CommandLineCommand : Command
    {
        public Core.CommandData CommandData { get; set; }

        public CommandLineCommand(Core.CommandData commandData)
            : base(commandData.Name!, commandData.Description)
        {
            CommandData = commandData;
        }
    }

    public class CommandBuilder : IBuilder<CommandLineCommand>
    {
        private readonly Core.CommandData _rootData;
        private readonly Func<IHost, Task> _commandHandler;
        public CommandBuilder(Core.CommandData rootData, Func<IHost, Task> commandHandler)
        {
            _rootData = rootData;
            _commandHandler = commandHandler;
        }

        public CommandLineCommand Build()
        {
            // Set root Name to avoid exceptions.
            _rootData.Name = "Commandir";
            
            CommandLineCommand rootCommand = new CommandLineCommand(_rootData);
            foreach(Core.CommandData subCommandData in _rootData.Commands)
            {
                AddCommand(subCommandData, rootCommand, _commandHandler);
            }

            return rootCommand;
        }

        private static void AddCommand(Core.CommandData commandData, Command parentCommand, Func<IHost, Task> commandHandler)
        {
            if(string.IsNullOrWhiteSpace(commandData.Name))
                throw new ArgumentNullException(nameof(Core.CommandData.Name));

            CommandLineCommand command = new CommandLineCommand(commandData);
            command.Handler = CommandHandler.Create<IHost>(commandHandler);
            parentCommand.AddCommand(command);

            foreach(Core.ArgumentData argumentData in commandData.Arguments)
            {
                if(string.IsNullOrWhiteSpace(commandData.Name))
                    throw new ArgumentNullException(nameof(Core.ArgumentData.Name));

                Argument argument = new Argument<string>(argumentData.Name, argumentData.Description);
                command.AddArgument(argument);
            }

            foreach(Core.OptionData optionData in commandData.Options)
            {
                 if(string.IsNullOrWhiteSpace(optionData.Name))
                    throw new ArgumentNullException(nameof(Core.OptionData.Name));

                Option option = new Option<string>($"--{optionData.Name}", optionData.Description) { IsRequired = optionData.Required };
                command.AddOption(option);
            }

            foreach(Core.CommandData subCommandData in commandData.Commands)
            {
                AddCommand(subCommandData, command, commandHandler);
            }
        }       
    }
}