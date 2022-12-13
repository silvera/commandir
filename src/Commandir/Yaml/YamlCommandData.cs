using Commandir.Interfaces;

namespace Commandir.Yaml;

public sealed class YamlCommandData : ICommandData
{   
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Executor { get; set; }
    public Dictionary<string, object?> Parameters { get; set; } = new Dictionary<string, object?>();
    public List<YamlArgumentData> Arguments { get; set; } = new List<YamlArgumentData>();
    public List<YamlOptionData> Options { get; set; } = new List<YamlOptionData>();
    public List<YamlCommandData> Commands { get; set; } = new List<YamlCommandData>();

    // Populated post-deserialization
    public YamlCommandData? Parent { get; set; }
    public string? Path { get; set;}
}

public sealed class YamlArgumentData
{
    public string? Name { get; set; }
    public string? Description { get; set; }
}

public sealed class YamlOptionData
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool Required { get; set; }
}