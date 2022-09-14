using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Commandir;

internal sealed class YamlCommandDataBuilder : IBuilder<Core.CommandData>
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

    public Core.CommandData Build()
    { 
        return _deserializer.Deserialize<Core.CommandData>(_yaml);  
    }
}