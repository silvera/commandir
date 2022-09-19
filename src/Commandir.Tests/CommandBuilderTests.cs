using Commandir.Core;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.Threading.Tasks;
using Xunit;

namespace Commandir.Tests
{
    public class CommandBuilderTests
    {
        private static CommandDefinition CreateRootCommandData() => new CommandDefinition() { Name = "root", Description = "root", Type = "Commandir.Builtins.Default" };        private static CommandDefinition CreateSubCommandData() => new CommandDefinition { Name = "subcommand", Description = "subcommand", Type = "Commandir.Builtins.Default" };
        private static CommandDefinition CreateSubSubCommandData() => new CommandDefinition { Name = "subsubcommand", Description = "subsubcommand", Type = "Commandir.Builtins.Default" };
        
        private CommandBuilder CreateCommandBuilder(CommandDefinition rootCommandDefinition)
        {
            ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            return new CommandBuilder(rootCommandDefinition, host => Task.CompletedTask, loggerFactory);
        }
        
        [Fact]
        public void Builds_CommandTree()
        {
            CommandDefinition rootCommandData = CreateRootCommandData();
            CommandDefinition subCommandData = CreateSubCommandData();    
            CommandDefinition subSubCommandData = CreateSubSubCommandData();
            
            subCommandData.Commands.Add(subSubCommandData);
            rootCommandData.Commands.Add(subCommandData);

            CommandLineCommand rootCommand = CreateCommandBuilder(rootCommandData).Build();
            Assert.NotNull(rootCommand);
            Assert.Equal(rootCommandData.Description,rootCommand.Description);

            Assert.Equal(1, rootCommand.Subcommands.Count);
            CommandLineCommand subCommand = (CommandLineCommand)rootCommand.Subcommands[0];
            Assert.NotNull(subCommand);
            Assert.Equal(subCommandData.Name, subCommand.Name);
            Assert.Equal(subCommandData.Description, subCommand.Description);

            Assert.Equal(1, subCommand.Subcommands.Count);
            CommandLineCommand subSubCommand = (CommandLineCommand)subCommand.Subcommands[0];
            Assert.NotNull(subSubCommand);
            Assert.Equal(subSubCommandData.Name, subSubCommand.Name);
            Assert.Equal(subSubCommandData.Description, subSubCommand.Description);
        }

        [Fact]
        public void Adds_Argument()
        {   
            CommandDefinition rootCommandData = CreateRootCommandData();
            CommandDefinition subCommandData = CreateSubCommandData();
            ArgumentDefinition argumentData = new ArgumentDefinition { Name = "argument", Description = "argument" }; 
            subCommandData.Arguments.Add(argumentData);
            rootCommandData.Commands.Add(subCommandData);

            CommandLineCommand rootCommand = CreateCommandBuilder(rootCommandData).Build();

            CommandLineCommand subCommand = (CommandLineCommand)rootCommand.Subcommands[0];
            Argument argument = subCommand.Arguments[0];
            Assert.Equal(argumentData.Name, argument.Name);
            Assert.Equal(argumentData.Description, argument.Description);
        }

        [Fact]
        public void Adds_Option()
        {   
            CommandDefinition rootCommandData = CreateRootCommandData();
            CommandDefinition subCommandData = CreateSubCommandData();
            OptionDefinition optionData = new OptionDefinition { Name = "option", Description = "option" };
            subCommandData.Options.Add(optionData);
            rootCommandData.Commands.Add(subCommandData);

            CommandLineCommand rootCommand = CreateCommandBuilder(rootCommandData).Build();

            CommandLineCommand subCommand = (CommandLineCommand)rootCommand.Subcommands[0];
            Option option = subCommand.Options[0];
            Assert.Equal(optionData.Name, option.Name);
            Assert.Equal(optionData.Description, option.Description);
        }
    }
}



