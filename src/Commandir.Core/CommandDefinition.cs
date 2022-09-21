namespace Commandir.Core;

public sealed class CommandDefinition
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Type { get; set; }
    public Dictionary<string, object?> Parameters { get; set; } = new Dictionary<string, object?>();
    public List<ArgumentDefinition> Arguments { get; set; } = new List<ArgumentDefinition>();
    public List<OptionDefinition> Options { get; set; } = new List<OptionDefinition>();
    public List<CommandDefinition> Commands { get; set; } = new List<CommandDefinition>();
}

public sealed class ArgumentDefinition
{
    public string? Name { get; set; }
    public string? Description { get; set; }
}

public sealed class OptionDefinition
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool Required { get; set; }
}