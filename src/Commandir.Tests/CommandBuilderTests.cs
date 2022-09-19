using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.Threading.Tasks;
using Xunit;

namespace Commandir.Tests
{
    public class CommandBuilderTests
    {
        private static Core.CommandData CreateRootCommandData() => new Core.CommandData() { Name = "root", Description = "root", Type = "Commandir.Builtins.Default" };
        private static Core.CommandData CreateSubCommandData() => new Core.CommandData { Name = "subcommand", Description = "subcommand", Type = "Commandir.Builtins.Default" };
        private static Core.CommandData CreateSubSubCommandData() => new Core.CommandData { Name = "subsubcommand", Description = "subsubcommand", Type = "Commandir.Builtins.Default" };
        
        private CommandBuilder CreateCommandBuilder(Core.CommandData rootCommandData)
        {
            ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            return new CommandBuilder(rootCommandData, host => Task.CompletedTask, loggerFactory);
        }
        
        [Fact]
        public void Builds_CommandTree()
        {
            Core.CommandData rootCommandData = CreateRootCommandData();
            Core.CommandData subCommandData = CreateSubCommandData();    
            Core.CommandData subSubCommandData = CreateSubSubCommandData();
            
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
            Core.CommandData rootCommandData = CreateRootCommandData();
            Core.CommandData subCommandData = CreateSubCommandData();
            Core.ArgumentData argumentData = new Core.ArgumentData{ Name = "argument", Description = "argument" }; 
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
            Core.CommandData rootCommandData = CreateRootCommandData();
            Core.CommandData subCommandData = CreateSubCommandData();
            Core.OptionData optionData = new Core.OptionData { Name = "option", Description = "option" };
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



