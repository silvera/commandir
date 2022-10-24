using Commandir;
using Commandir.Core;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.IO;
using Xunit;

namespace Commandir.Tests
{
    public class CommandLineCommandProviderTests
    {
        [Fact]
        public void FromFile()
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "Commandir.yaml");
            Result<CommandDefinition> rootDefinition = new YamlCommandDefinitionProvider()
                .FromFile(filePath);

            Assert.False(rootDefinition.HasError);

            ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            Result<CommandLineCommand> rootCommand = new CommandLineCommandProvider()
                .FromCommandDefinition(rootDefinition.Value, loggerFactory);

            Assert.False(rootCommand.HasError);

            ValidateCommand(rootCommand.Value);
        }

        private void ValidateCommand(CommandLineCommand root)
        {
            Assert.Equal("root", root.Description);
            
            CommandLineCommand command1 = (CommandLineCommand)root.Subcommands[0];
            Assert.Equal("command1", command1.Name);
            Assert.Equal("command1", command1.Description);

            CommandLineCommand command2 = (CommandLineCommand)command1.Subcommands[0];
            Assert.Equal("command2", command2.Name);
            Assert.Equal("command2", command2.Description);
            
            Argument argument = command2.Arguments[0];
            Assert.Equal("argument2", argument.Name);
            Assert.Equal("argument2", argument.Description);

            Option option = command2.Options[0];
            Assert.Equal("option2", option.Name);
            Assert.Equal("option2", option.Description);
            Assert.True(option.IsRequired);
        }
    }
}



