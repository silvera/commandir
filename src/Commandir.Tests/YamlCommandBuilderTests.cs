using Xunit;
using Commandir;
using System.CommandLine;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

namespace Commandir.Tests;

public class YamlCommandBuilderTests
{
    private async Task AssertExceptionAsync(string yaml, string? message = null)
    {
        var reader = new StringReader(yaml);
        var builder = new YamlCommandBuilder(reader);
        var ex = await Record.ExceptionAsync(() =>
        {
            var command = builder.Build(host => Task.CompletedTask);
            return Task.CompletedTask;
        });
        //Assert.IsType<YamlException>(ex);
        if(!string.IsNullOrWhiteSpace(message))
        {
            Assert.Equal(message, ex.Message);
        }
    }

    [Fact]
    public async Task ParseError_MalformedDocument()
    {
        const string yaml = @"---
           description: Test Commands
            commands:
        ";

        await AssertExceptionAsync(yaml);
    }
    [Fact]
    public async Task TopLevel_IsDictionary()
    {
        const string yaml = @"---
            - item 1
            - item2
        ";
        await AssertExceptionAsync(yaml, "Top-level must be a dictionary.");
    }

    [Fact]
    public async Task TopLevel_HasDescription()
    {
        const string yaml = @"---
            commands:
               - item1
               - item2
        ";

        await AssertExceptionAsync(yaml, "Top-level dictionary is missing a `description` entry.");
    }

    [Fact]
    public async Task TopLevel_HasCommands()
    {
        const string yaml = @"---
            description: Missing Commands
        ";

        await AssertExceptionAsync(yaml, "Top-level dictionary is missing a `commands` list.");
    }

    [Fact]
    public async Task Command_HasName()
    {
        const string yaml = @"---
            description: Commands List
            commands:
               - description: Description1
        ";

        await AssertExceptionAsync(yaml, "Command is missing a `name` entry.");
    }

    [Fact]
    public async Task Command_HasDescription()
    {
        const string yaml = @"---
            description: Commands List
            commands:
               - name: Command1
        ";

        await AssertExceptionAsync(yaml, "Command is missing a `description` entry.");
    }

    [Fact]
    public async Task Command_HasActions()
    {
        const string yaml = @"---
            description: Commands List
            commands:
               - name: Command1
                 description: Command1 description.
        ";

        await AssertExceptionAsync(yaml, "Command is missing an `actions` list.");
    }

    [Fact]
    public async Task Action_HasName()
    {
        const string yaml = @"---
            description: Commands List
            commands:
               - name: Command1
                 description: Command1 description.
                 actions:
                    - shell: bash
        ";

        await AssertExceptionAsync(yaml, "Action is missing a `name` entry.");
    }

    [Fact]
    public async Task Argument_HasName()
    {
        const string yaml = @"---
            description: Commands List
            commands:
               - name: Command1
                 description: Command1 description.
                 actions:
                    - name: Action1
                 arguments:
                    - description: Argument description.
                 
        ";

        await AssertExceptionAsync(yaml, "Argument is missing a `name` entry.");
    }

    [Fact]
    public async Task Argument_HasDescription()
    {
        const string yaml = @"---
            description: Commands List
            commands:
               - name: command
                 description: command1 description.
                 actions:
                    - name: action
                 arguments:
                    - name: argument name.
                 
        ";

        await AssertExceptionAsync(yaml, "Argument is missing a `description` entry.");
    }

    [Fact]
    public async Task Option_HasName()
    {
        const string yaml = @"---
            description: Commands List
            commands:
               - name: command
                 description: command1 description.
                 actions:
                    - name: action
                 options:
                    - description: description.       
        ";

        await AssertExceptionAsync(yaml, "Option is missing a `name` entry.");
    }

    [Fact]
    public async Task Option_HasDescription()
    {
        const string yaml = @"---
            description: Commands List
            commands:
               - name: command
                 description: command1 description.
                 actions:
                    - name: action
                 options:
                    - name: name.       
        ";

        await AssertExceptionAsync(yaml, "Option is missing a `description` entry.");
    }

    [Fact]
    public async Task Option_HasValidRequired()
    {
        const string yaml = @"---
            description: Commands List
            commands:
               - name: command
                 description: command1 description.
                 actions:
                    - name: action
                 options:
                    - name: name
                      description: description
                      required: foobar
        ";

        await AssertExceptionAsync(yaml, "Option is missing a valid `required` entry.");
    }

    [Fact]
    public void Validate_Command()
    {
        const string yaml = @"---
            description: Commands
            commands:
               - name: Command
                 description: Command description.
                 actions:
                    - name: Action
                      message: Message
        ";

        StringReader reader = new StringReader(yaml);
        YamlCommandBuilder builder = new YamlCommandBuilder(reader);
        Command rootCommand = builder.Build(host => Task.CompletedTask);
        Assert.Equal("Commands", rootCommand.Description);
        ActionCommand command = (ActionCommand)rootCommand.Subcommands.First();
        Assert.Equal("Command", command.Name);
        Assert.Equal("Command description.", command.Description);
        ActionData action = command.Actions.First();
        Assert.Equal("Action", action.Name);
        Assert.Equal("Message", action["message"]);
    }

    [Fact]
    public void Validate_CommandWithArgument()
    {
        const string yaml = @"---
            description: Commands
            commands:
               - name: Command
                 description: Command description.
                 actions:
                    - name: Action
                      message: Message
                 arguments:
                    - name: Argument
                      description: Argument description.
        ";

        StringReader reader = new StringReader(yaml);
        YamlCommandBuilder builder = new YamlCommandBuilder(reader);
        Command rootCommand = builder.Build(host => Task.CompletedTask);
        ActionCommand command = (ActionCommand)rootCommand.Subcommands.First();
        Argument argument = command.Arguments.First();
        Assert.Equal("Argument", argument.Name);
        Assert.Equal("Argument description.", argument.Description);
    }

    [Fact]
    public void Validate_CommandWithOption()
    {
        const string yaml = @"---
            description: Commands
            commands:
               - name: Command
                 description: Command description.
                 actions:
                    - name: Action
                      message: Message
                 options:
                    - name: Option
                      description: Option description.
                      required: true
        ";

        StringReader reader = new StringReader(yaml);
        YamlCommandBuilder builder = new YamlCommandBuilder(reader);
        Command rootCommand = builder.Build(host => Task.CompletedTask);
        ActionCommand command = (ActionCommand)rootCommand.Subcommands.First();
        Option option = command.Options.First();
        Assert.Equal("Option", option.Name);
        Assert.Equal("Option description.", option.Description);
        Assert.True(option.IsRequired);
    }
}