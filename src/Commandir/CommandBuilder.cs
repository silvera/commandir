using Microsoft.Extensions.Hosting;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;

namespace Commandir
{
    public class CommandBuilder : IBuilder<Command>
    {
        private readonly CommandData _rootCommandData;
        private readonly Func<IHost, Task> _commandHandler;
        public CommandBuilder(CommandData rootCommandData, Func<IHost, Task> commandHandler)
        {
            _rootCommandData = rootCommandData;
            _commandHandler = commandHandler;
        }

        public Command Build()
        {
            ActionCommand rootCommmandHolder = new ActionCommand("unused", "unused");
            foreach(CommandData commandData in _rootCommandData.Commands)
            {
                AddCommand(commandData, rootCommmandHolder);
            }

            RootCommand rootCommand = new RootCommand(_rootCommandData.Description);
            foreach(Command command in rootCommmandHolder.Subcommands)
            {
                rootCommand.AddCommand(command);
            }

            return rootCommand;
        }

        private void AddCommand(CommandData commandData, ActionCommand parent)
        {
            ActionCommand command = new ActionCommand(commandData.Name, commandData.Description);
            command.Handler = CommandHandler.Create<IHost>(_commandHandler);
            foreach(ArgumentData argumentData in commandData.Arguments)
            {
                command.AddArgument(new Argument<string>(argumentData.Name, argumentData.Description));
            }

            foreach(OptionData optionData in commandData.Options)
            {
                Option<string> option = new Option<string>(optionData.Name, optionData.Description)
                {
                    IsRequired = optionData.IsRequired
                };
                command.AddOption(option);
            }

            foreach(ActionData actionData in commandData.Actions)
            {
                command.AddAction(actionData);
            }

            foreach(CommandData subCommandData in commandData.Commands)
            {
                AddCommand(subCommandData, command);
            }

            parent.AddCommand(command);
        }
    }
}