using System.IO;
using Xunit;

namespace Commandir.Tests;

public class YamlCommandDataBuilderTests
{
    private void AssertException(string yaml, string message)
    {
        var reader = new StringReader(yaml);
        var builder = new YamlCommandDataBuilder(reader);
        var ex = Record.Exception(() =>  { builder.Build(); });
        Assert.Equal(message, ex.Message);
    }

    [Fact]
    public void Document_IsDictionary()
    {
        const string yaml = @"---
            - item 1
            - item2
        ";

        AssertException(yaml, "Top-level must be a dictionary.");
    }

    [Fact]
    public void Document_HasDescription()
    {
        const string yaml = @"---
            commands:
               - item1
               - item2
        ";

        AssertException(yaml, "Top-level dictionary is missing a `description` entry.");
    }

    [Fact]
    public void Document_HasCommands()
    {
        const string yaml = @"---
            description: Missing Commands
        ";

        AssertException(yaml, "Top-level dictionary is missing a `commands` list.");
    }

    [Fact]
    public void Command_HasName()
    {
        const string yaml = @"---
            description: Commands List
            commands:
               - description: Description1
        ";

        AssertException(yaml, "Command is missing a `name` entry.");
    }

    [Fact]
    public void Command_HasDescription()
    {
        const string yaml = @"---
            description: Commands List
            commands:
               - name: Command1
        ";

        AssertException(yaml, "Command is missing a `description` entry.");
    }

    [Fact]
    public void Command_HasActions()
    {
        const string yaml = @"---
            description: Commands List
            commands:
               - name: Command1
                 description: Command1 description.
        ";

        AssertException(yaml, "Command is missing an `actions` list.");
    }

    [Fact]
    public void Action_HasName()
    {
        const string yaml = @"---
            description: Commands List
            commands:
               - name: Command1
                 description: Command1 description.
                 actions:
                    - shell: bash
        ";

        AssertException(yaml, "Action is missing a `name` entry.");
    }

    [Fact]
    public void Argument_HasName()
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

        AssertException(yaml, "Argument is missing a `name` entry.");
    }

    [Fact]
    public void Argument_HasDescription()
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

        AssertException(yaml, "Argument is missing a `description` entry.");
    }

    [Fact]
    public void Option_HasName()
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

        AssertException(yaml, "Option is missing a `name` entry.");
    }

    [Fact]
    public void Option_HasDescription()
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

        AssertException(yaml, "Option is missing a `description` entry.");
    }

    [Fact]
    public void Option_HasValidRequired()
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

        AssertException(yaml, "Option is missing a valid `required` entry.");
    }
}