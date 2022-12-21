using System.CommandLine;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Commandir.Commands;

public sealed class YamlCommandBuilder
{
    private readonly string _yaml;
    private readonly IDeserializer _deserializer;
    
    public YamlCommandBuilder(string yaml)
    {
        _yaml = yaml;
        _deserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .WithNamingConvention(CamelCaseNamingConvention.Instance) 
            .Build();
    }

    public CommandWithData Build()
    {
        CommandData rootCommandData = _deserializer.Deserialize<CommandData>(_yaml); 
        rootCommandData.Name = "Commandir";
        
        CommandWithData rootCommand = new CommandWithData(rootCommandData);
        rootCommand.AddOption(new Option<bool>("--verbose", "Enables verbose logging."));
        foreach(CommandData subData in rootCommandData.Commands)
        {
            AddCommand(subData, rootCommand);
        }

        return rootCommand;
    }
    
    private static void AddCommand(CommandData commandData, CommandWithData parentCommand)
    {
        if(string.IsNullOrWhiteSpace(commandData.Name))
            throw new Exception("Command `name` is null");

        var command = new CommandWithData(commandData);
        parentCommand.AddCommand(command);

        foreach(ArgumentData argumentData in commandData.Arguments)
        {
            if(string.IsNullOrWhiteSpace(argumentData.Name))
                throw new Exception("Argument `name` is null");
            
            var argument = new Argument<string>(argumentData.Name, argumentData.Description);
            command.AddArgument(argument);
        }

        foreach(OptionData optionData in commandData.Options)
        {
            if(string.IsNullOrWhiteSpace(optionData.Name))
                throw new Exception("Option `name` is null");

            var option = new Option<string>($"--{optionData.Name}", optionData.Description) { IsRequired = optionData.Required };
            command.AddOption(option);
        }

        foreach(CommandData subCommandData in commandData.Commands)
        {
            AddCommand(subCommandData, command);
        }
    }
}