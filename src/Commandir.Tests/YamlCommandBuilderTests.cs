using Xunit;
using Commandir;
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
}