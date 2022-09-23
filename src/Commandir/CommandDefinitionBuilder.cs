using Commandir.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Commandir;

internal sealed class CommandDefinitionBuilder
{
    private CommandDefinition? _rootDefinition;

    public CommandDefinitionBuilder AddYamlFile(string filePath)
    {
        string yaml = File.ReadAllText(filePath);
        return AddYaml(yaml);
    }

    public CommandDefinitionBuilder AddYaml(string yaml)
    {
        var deserializer  = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .WithNamingConvention(CamelCaseNamingConvention.Instance) 
            .Build();

        _rootDefinition = deserializer.Deserialize<CommandDefinition>(yaml);
        _rootDefinition.Name = "Commandir";
        return this;
    }

    public CommandDefinition? Build() => _rootDefinition;
}