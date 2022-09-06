using System.CommandLine;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Commandir.Tests
{
    public class CommandBuilderTests
    {
        private static CommandData CreateRootCommandData() => new CommandData("root", "root");
        private static CommandData CreateSubCommandData() => new CommandData("subcommand", "subcommand");
        private static CommandData CreateSubSubCommandData() => new CommandData("subsubcommand", "subsubcommand");
        private CommandBuilder CreateCommandBuilder(CommandData rootCommandData) => new CommandBuilder(rootCommandData, host => Task.CompletedTask);

        [Fact]
        public void Builds_CommandTree()
        {
            CommandData rootCommandData = CreateRootCommandData();
            CommandData subCommandData = CreateSubCommandData();    
            CommandData subSubCommandData = CreateSubSubCommandData();
            
            subCommandData.AddCommand(subSubCommandData);
            rootCommandData.AddCommand(subCommandData);

            Command rootCommand = CreateCommandBuilder(rootCommandData).Build();
            Assert.NotNull(rootCommand);
            Assert.Equal(rootCommandData.Description,rootCommand.Description);

            Assert.Equal(1, rootCommand.Subcommands.Count);
            Command subCommand = rootCommand.Subcommands.First();
            Assert.NotNull(subCommand);
            Assert.Equal(subCommandData.Name, subCommand.Name);
            Assert.Equal(subCommandData.Description, subCommand.Description);

            Assert.Equal(1, subCommand.Subcommands.Count);
            Command subSubCommand = subCommand.Subcommands.First();
            Assert.NotNull(subSubCommand);
            Assert.Equal(subSubCommandData.Name, subSubCommand.Name);
            Assert.Equal(subSubCommandData.Description, subSubCommand.Description);
        }

        [Fact]
        public void Adds_Argument()
        {   
            CommandData rootCommandData = CreateRootCommandData();
            CommandData subCommandData = CreateSubCommandData();
            ArgumentData argumentData = new ArgumentData("argument", "argument"); 
            subCommandData.AddArgument(argumentData);
            rootCommandData.AddCommand(subCommandData);

            Command rootCommand = CreateCommandBuilder(rootCommandData).Build();

            Command subCommand = rootCommand.Subcommands.First();
            Argument argument = subCommand.Arguments.First();
            Assert.Equal(argumentData.Name, argument.Name);
            Assert.Equal(argumentData.Description, argument.Description);
        }

        [Fact]
        public void Adds_Option()
        {   
            CommandData rootCommandData = CreateRootCommandData();
            CommandData subCommandData = CreateSubCommandData();
            OptionData optionData = new OptionData("option", "option", true);
            subCommandData.AddOption(optionData);
            rootCommandData.AddCommand(subCommandData);

            Command rootCommand = CreateCommandBuilder(rootCommandData).Build();

            Command subCommand = rootCommand.Subcommands.First();
            Option option = subCommand.Options.First();
            Assert.Equal(optionData.Name, option.Name);
            Assert.Equal(optionData.Description, option.Description);
        }

        [Fact]
        public void Adds_Action()
        {   
            CommandData rootCommandData = CreateRootCommandData();
            CommandData subCommandData = CreateSubCommandData();
            ActionData actionData = new ActionData("action");
            subCommandData.AddAction(actionData);
            rootCommandData.AddCommand(subCommandData);

            Command rootCommand = CreateCommandBuilder(rootCommandData).Build();

            ActionCommand subCommand = (ActionCommand)rootCommand.Subcommands.First();
            ActionData action = subCommand.Actions.First();
            Assert.Equal(actionData.Name, action.Name);
        }
    }
}



