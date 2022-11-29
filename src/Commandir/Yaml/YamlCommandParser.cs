using Commandir.Commands;
using System.CommandLine;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Commandir.Yaml;

public class YamlCommandParser
{
    public sealed class YamlParseException : Exception
    {
        public YamlParseException(string message)
            : base(message)
        {
        }
    }

    internal static CommandirCommand Parse(string yaml)
    {
        var deserializer  = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .WithNamingConvention(CamelCaseNamingConvention.Instance) 
            .Build();

        YamlCommandData rootData = deserializer.Deserialize<YamlCommandData>(yaml);
        rootData.Name = "Commandir";

        CommandirCommand rootCommand = new CommandirCommand(rootData.Name, rootData.Description, rootData.Type, rootData.Parameters);
        foreach(YamlCommandData subData in rootData.Commands)
        {
            AddCommand(subData, rootCommand);
        }


        return rootCommand;
    }

    private static void AddCommand(YamlCommandData commandData, CommandirCommand parentCommand)
    {
        if(string.IsNullOrWhiteSpace(commandData.Name))
            throw new YamlParseException("Command `name` is null");

        if(string.IsNullOrWhiteSpace(commandData.Type))
            throw new YamlParseException("Command `action` is null");

        var command = new CommandirCommand(commandData.Name, commandData.Description, commandData.Type, commandData.Parameters);
        parentCommand.AddCommand(command);

        foreach(YamlArgumentData argumentDefinition in commandData.Arguments)
        {
            if(string.IsNullOrWhiteSpace(argumentDefinition.Name))
                throw new YamlParseException("Argument `name` is null");
            
            var argument = new Argument<string>(argumentDefinition.Name, argumentDefinition.Description);
            command.AddArgument(argument);
        }

        foreach(YamlOptionData optionDefinition in commandData.Options)
        {
            if(string.IsNullOrWhiteSpace(optionDefinition.Name))
                throw new YamlParseException("Option `name` is null");

            var option = new Option<string>($"--{optionDefinition.Name}", optionDefinition.Description) { IsRequired = optionDefinition.Required };
            command.AddOption(option);
        }

        foreach(YamlCommandData subCommandData in commandData.Commands)
        {
            AddCommand(subCommandData, command);
        }
    }
}