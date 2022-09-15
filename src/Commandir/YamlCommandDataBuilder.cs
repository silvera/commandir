using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

using Commandir.Core;

namespace Commandir;

public sealed class YamlCommandDataBuilder : IBuilder<Core.CommandData>
{
    private readonly string _yaml;
    private readonly IDeserializer _deserializer;
    public YamlCommandDataBuilder(string yaml)
    {
        _yaml = yaml;
        _deserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .WithNamingConvention(CamelCaseNamingConvention.Instance) 
            .Build();
    }

    public CommandData Build()
    { 
        CommandData rootData = _deserializer.Deserialize<CommandData>(_yaml);

        // The yaml file is not required to contain an entry for the root command's name.
        if(string.IsNullOrWhiteSpace(rootData.Name)) 
            rootData.Name = "Commandir";

        return rootData; 
    }
}