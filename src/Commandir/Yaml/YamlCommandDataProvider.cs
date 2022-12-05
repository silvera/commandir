using Commandir.Interfaces;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Commandir.Yaml;

public sealed class YamlCommandDataProvider : ICommandDataProvider<YamlCommandData>
{
    private readonly YamlCommandData _rootCommand;
    private readonly Dictionary<string, YamlCommandData> _commandsByPath = new Dictionary<string, YamlCommandData>(StringComparer.OrdinalIgnoreCase);
    
    public YamlCommandDataProvider(string yaml)
    {
        var deserializer  = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .WithNamingConvention(CamelCaseNamingConvention.Instance) 
            .Build();

        _rootCommand = deserializer.Deserialize<YamlCommandData>(yaml);
        foreach(YamlCommandData subCommand in _rootCommand.Commands)
        {
            StringBuilder pathBuilder = new StringBuilder();
            AddCommand(subCommand, pathBuilder);
        }
    }

    private void AddCommand(YamlCommandData command, StringBuilder pathBuilder)
    {
        pathBuilder.Append($"/{command.Name}");
        string path = pathBuilder.ToString();
        _commandsByPath[path] = command;

        foreach(YamlCommandData subCommand in command.Commands)
        {
            AddCommand(subCommand, pathBuilder);
        }
    }

    public YamlCommandData? GetRootCommandData()
    {
        return _rootCommand;
    }

    public YamlCommandData? GetCommandData(string path)
    {
        return _commandsByPath.TryGetValue(path, out YamlCommandData? command) ? command : null;
    }
}

