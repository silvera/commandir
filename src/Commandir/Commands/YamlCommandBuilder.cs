using Commandir.Yaml;
using System.CommandLine;

namespace Commandir.Commands;

internal static class YamlCommandBuilder
{
    internal static Command Build(YamlCommandData rootData)
    {
        var rootCommand = new Command("Commandir", rootData.Description);
        foreach(YamlCommandData subData in rootData.Commands)
        {
            AddCommand(subData, rootCommand);
        }

        return rootCommand;
    }

    private static void AddCommand(YamlCommandData commandData, Command parentCommand)
    {
        if(string.IsNullOrWhiteSpace(commandData.Name))
            throw new Exception("Command `name` is null");

        var command = new Command(commandData.Name, commandData.Description);
        parentCommand.AddCommand(command);

        foreach(YamlArgumentData argumentDefinition in commandData.Arguments)
        {
            if(string.IsNullOrWhiteSpace(argumentDefinition.Name))
                throw new Exception("Argument `name` is null");
            
            var argument = new Argument<string>(argumentDefinition.Name, argumentDefinition.Description);
            command.AddArgument(argument);
        }

        foreach(YamlOptionData optionDefinition in commandData.Options)
        {
            if(string.IsNullOrWhiteSpace(optionDefinition.Name))
                throw new Exception("Option `name` is null");

            var option = new Option<string>($"--{optionDefinition.Name}", optionDefinition.Description) { IsRequired = optionDefinition.Required };
            command.AddOption(option);
        }

        foreach(YamlCommandData subCommandData in commandData.Commands)
        {
            AddCommand(subCommandData, command);
        }
    }
}