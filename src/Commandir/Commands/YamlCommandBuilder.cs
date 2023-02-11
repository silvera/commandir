using System.CommandLine;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Commandir.Commands;

/// <summary>
/// Builds a tree of System.CommandLine commands based on the yaml-formatted command definitions.
/// </summary>
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

    /// <summary>
    /// Returns the root command. 
    /// </summary>
    internal CommandWithData Build()
    {
        CommandData rootCommandData = _deserializer.Deserialize<CommandData>(_yaml); 
        rootCommandData.Name = rootCommandData.Name ?? "Commandir";

        rootCommandData.Options.Add(new OptionData
        {
            Name = "verbose",
            ShortName = "v",
            Description = "Enables verbose logging.",
            Type = "bool"
        });

        CommandWithData rootCommand = new CommandWithData(rootCommandData);
        AddCommand(rootCommandData, null, rootCommand);
        return rootCommand;
    }
    
    private static void AddCommand(CommandData commandData, CommandWithData? parentCommand = null, CommandWithData? commandOverride = null)
    {
        if(string.IsNullOrWhiteSpace(commandData.Name))
            throw new Exception("Command `name` is null");

        var command = commandOverride ?? new CommandWithData(commandData);
        parentCommand?.AddCommand(command);

        foreach(ArgumentData argumentData in commandData.Arguments)
        {    
            AddArgument(command, argumentData);
        }

        foreach(OptionData optionData in commandData.Options)
        {
            AddOption(command, optionData);
        }

        foreach(CommandData subCommandData in commandData.Commands)
        {
            AddCommand(subCommandData, command);
        }
    }

    private static void AddArgument(CommandWithData command, ArgumentData data)
    {
        if(string.IsNullOrWhiteSpace(data.Name))
            throw new Exception("Argument `name` is null");

        string name = data.Name;
        string? description = data.Description;
        Argument argument = data.Type switch
        {
            "ushort" => new Argument<ushort>(name, description),
            "long" => new Argument<long>(name, description),
            "int" => new Argument<int>(name, description),
            "uint" => new Argument<uint>(name, description),
            "short" => new Argument<short>(name, description),
            "ulong" => new Argument<ulong>(name, description),
            "float" => new Argument<float>(name, description),
            "double" => new Argument<double>(name, description),
            "bool" => new Argument<bool>(name, description),
            "string" => new Argument<string>(name, description),
            _ => new Argument<string>(name, description)
        };

        command.AddArgument(argument);
    }

    private static void AddOption(CommandWithData command, OptionData data)
    {
        if(string.IsNullOrWhiteSpace(data.Name))
            throw new Exception("Option `name` is null");

        string name = $"--{data.Name}";
        string? description = data.Description;
        bool required = data.Required;
        Option option = data.Type switch
        {
            "short" => new Option<short>(name, description) { IsRequired = data.Required },
            "ushort" => new Option<ushort>(name, description) { IsRequired = data.Required },
            "int" => new Option<int>(name, description) { IsRequired = data.Required },
            "uint" => new Option<uint>(name, description) { IsRequired = data.Required },
            "long" => new Option<long>(name, description) { IsRequired = data.Required },
            "ulong" => new Option<ulong>(name, description) { IsRequired = data.Required },
            "float" => new Option<float>(name, description) { IsRequired = data.Required },
            "double" => new Option<double>(name, description) { IsRequired = data.Required },
            "bool" => new Option<bool>(name, description) { IsRequired = data.Required },
            "string" => new Option<string>(name, description) { IsRequired = data.Required },
            _ => new Option<string>(name, description) { IsRequired = data.Required }
        };

        if(data.ShortName is not null)
        {
            option.AddAlias($"-{data.ShortName}");
        }

        command.AddOption(option);
    }
}