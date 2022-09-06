using Microsoft.Extensions.Hosting;
using System.CommandLine;

namespace Commandir
{
    public abstract class CommandBuilder
    {
        public abstract Command Build(Func<IHost, Task> handler);
    }

    public class CommandDataBuilder
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

        public Command Build(CommandData rootCommandData)
        {
            ActionCommand rootCommmandHolder = new ActionCommand("unused", "unused");
            foreach(CommandData commandData in rootCommandData.Commands)
            {
                AddCommand(commandData, rootCommmandHolder);
            }

            RootCommand rootCommand = new RootCommand(rootCommandData.Description);
            foreach(Command command in rootCommmandHolder.Subcommands)
            {
                rootCommand.AddCommand(command);
            }

            return rootCommand;
        }
    }
}