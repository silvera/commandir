using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

using Commandir.Core;

namespace Commandir;

public sealed class YamlCommandDefinitionBuilder : IBuilder<CommandDefinition>
{
    private readonly string _yaml;
    private readonly IDeserializer _deserializer;
    public YamlCommandDefinitionBuilder(string yaml)
    {
        _yaml = yaml;
        _deserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .WithNamingConvention(CamelCaseNamingConvention.Instance) 
            .Build();
    }

    public CommandDefinition Build()
    { 
        CommandDefinition rootDefinition = _deserializer.Deserialize<CommandDefinition>(_yaml);

        // The yaml file is not required to contain an entry for the root command's name.
        if(string.IsNullOrWhiteSpace(rootDefinition.Name)) 
            rootDefinition.Name = "Commandir";

        return rootDefinition; 
    }
}