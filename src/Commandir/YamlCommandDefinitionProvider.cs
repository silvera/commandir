using Commandir.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Commandir;

internal sealed class YamlCommandDefinitionProvider
{
    public Result<CommandDefinition> FromFile(string filePath)
    {
        string data = File.ReadAllText(filePath);
        return FromString(data);
    }

    public Result<CommandDefinition> FromString(string data)
    {
        var deserializer  = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .WithNamingConvention(CamelCaseNamingConvention.Instance) 
            .Build();

        CommandDefinition definition = deserializer.Deserialize<CommandDefinition>(data);
        definition.Name = "Commandir";
        return Result<CommandDefinition>.Ok(definition);
    }
}