using Commandir.Commands;
using Commandir.Yaml;
using System.Collections.Generic;
using System.CommandLine;
using Xunit;

namespace Commandir.Tests;

public class YamlCommandParserTests
{
    [Fact]
    public void Parse()
    {
        string yaml = @"---
                description: root description
                commands:
                   - name: command1
                     description: command1 description
                     type: commandir.actions.noop
                     commands:
                        - name: command2
                          description: command2 description
                          type: commandir.actions.noop
                          parameters:
                             parameter2: parameter2 value
                          arguments:
                             - name: argument2
                               description: argument2 description
                          options:
                             - name: option2
                               description: option2 description
                               required: false
            ";
        
        var rootCommand = YamlCommandParser.Parse(yaml);

        Assert.Equal("root description", rootCommand.Description);
            
        CommandirCommand command1 = (CommandirCommand)rootCommand.Subcommands[0];
        Assert.Equal("command1", command1.Name);
        Assert.Equal("command1 description", command1.Description);
        Assert.Equal("commandir.actions.noop", command1.Action);
       
        CommandirCommand command2 = (CommandirCommand)command1.Subcommands[0];
        Assert.Equal("command2", command2.Name);
        Assert.Equal("command2 description", command2.Description);

        Assert.Equal(new Dictionary<string, object?>() { {"parameter2", "parameter2 value"}}, command2.Parameters);

        Argument argument = command2.Arguments[0];
        Assert.Equal("argument2", argument.Name);
        Assert.Equal("argument2 description", argument.Description);

        Option option = command2.Options[0];
        Assert.Equal("option2", option.Name);
        Assert.Equal("option2 description", option.Description);
        Assert.False(option.IsRequired);
    }
}