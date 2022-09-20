using Commandir.Core;
using System.IO;
using Xunit;

namespace Commandir.Tests
{
    public class CommandDefinitionBuilderTests
    {
        [Fact]
        public void FromString()
        {
            string yaml = @"---
                description: root
                commands:
                   - name: command1
                     description: command1
                     commands:
                        - name: command2
                          description: command2
                          type: Commandir.Builtins.Echo
                          arguments:
                             - name: argument2
                               description: argument2
                          options:
                             - name: option2
                               description: option2
                               required: true
            ";

            CommandDefinition root = new CommandDefinitionBuilder()
            .AddYaml(yaml)
            .Build()!;
            
            ValidateCommandDefinition(root);
        }

        [Fact]
        public void FromFile()
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "Commandir.yaml");
            CommandDefinition root = new CommandDefinitionBuilder()
            .AddYamlFile(filePath)
            .Build()!;

            ValidateCommandDefinition(root);
        }

        private void ValidateCommandDefinition(CommandDefinition root)
        {
            Assert.Equal("root", root.Description);
            
            CommandDefinition command1 = root.Commands[0];
            Assert.Equal("command1", command1.Name);
            Assert.Equal("command1", command1.Description);

            CommandDefinition command2 = command1.Commands[0];
            Assert.Equal("command2", command2.Name);
            Assert.Equal("command2", command2.Description);
            
            ArgumentDefinition argument = command2.Arguments[0];
            Assert.Equal("argument2", argument.Name);
            Assert.Equal("argument2", argument.Description);

            OptionDefinition option = command2.Options[0];
            Assert.Equal("option2", option.Name);
            Assert.Equal("option2", option.Description);
            Assert.True(option.Required);
        }
    }
}
