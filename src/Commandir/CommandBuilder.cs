using Microsoft.Extensions.Hosting;
using System.CommandLine;

namespace Commandir
{
    public class CommandBuilder : IBuilder<Command>
    {
        private static void AddCommand(CommandData commandData, ActionCommand parent)
        {
            ActionCommand command = new ActionCommand(commandData.Name, commandData.Description);
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

        private readonly CommandData _rootCommandData;
        public CommandBuilder(CommandData rootCommandData)
        {
            _rootCommandData = rootCommandData;
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
    }
}