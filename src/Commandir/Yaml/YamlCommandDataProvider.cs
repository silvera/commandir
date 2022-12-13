using Commandir.Interfaces;
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
        AssignParents(_rootCommand);
        AssignPaths(_rootCommand);
    }

    private void AssignParents(YamlCommandData command, YamlCommandData? parentCommand = null)
    {
        command.Parent = parentCommand;
        foreach(var subCommand in command.Commands)
        {
            AssignParents(subCommand, command);
        }
    }

    private void AssignPaths(YamlCommandData command)
    {
        string commandPath = GetPath(command);
        command.Path = commandPath;
        _commandsByPath[commandPath] = command;
        foreach(var subCommand in command.Commands)
        {
            AssignPaths(subCommand);
        }
    }

    private static string GetPath(YamlCommandData command)
    {
        List<string> components = new List<string>();
        GetPathComponents(command, components);
        components.Reverse();
        return string.Concat(components);
    }

    private static void GetPathComponents(YamlCommandData command, List<string> names)
    {
        if(command.Name != null)
            names.Add($"/{command.Name}");
        
        if(command.Parent != null)
        {
            GetPathComponents(command.Parent!, names);
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

