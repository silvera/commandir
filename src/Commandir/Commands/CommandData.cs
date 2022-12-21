namespace Commandir.Commands;

public sealed class CommandData
{   
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Executor { get; set; }
    public Dictionary<string, object?> Parameters { get; set; } = new Dictionary<string, object?>();
    public List<ArgumentData> Arguments { get; set; } = new List<ArgumentData>();
    public List<OptionData> Options { get; set; } = new List<OptionData>();
    public List<CommandData> Commands { get; set; } = new List<CommandData>();
}

public sealed class ArgumentData
{
    public string? Name { get; set; }
    public string? Description { get; set; }
}

public sealed class OptionData
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool Required { get; set; }
}