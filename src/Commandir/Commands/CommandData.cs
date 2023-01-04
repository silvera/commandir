namespace Commandir.Commands;

/// <summary>
/// Represents the data required for a command.
/// </summary>
public sealed class CommandData
{   
    /// <summary>
    /// The command name (required).
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// The command description (optional).
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The executor that should be used to execute this command (required). 
    /// </summary>
    public string? Executor { get; set; }

    /// <summary>
    /// Parameters used by the executor (optional).
    /// </summary>
    public Dictionary<string, object?> Parameters { get; set; } = new Dictionary<string, object?>();
    
    /// <summary>
    /// The command arguments (optional).
    /// </summary>
    public List<ArgumentData> Arguments { get; set; } = new List<ArgumentData>();
    
    /// <summary>
    /// The command options (optional).
    /// </summary>
    public List<OptionData> Options { get; set; } = new List<OptionData>();
   
    /// <summary>
    /// The command subcommands (optional).
    /// </summary>
    public List<CommandData> Commands { get; set; } = new List<CommandData>();
}

/// <summary>
/// Represents the data required for a command argument.
/// </summary>
public sealed class ArgumentData
{
    /// <summary>
    /// The argument name (required).
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// The argument description (optional).
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The argument type (optional).
    /// </summary>
    public string? Type { get; set;}
}

/// <summary>
/// Represents the data required for a command option.
/// </summary>
public sealed class OptionData
{
    /// <summary>
    /// The option name (required).
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The option short name (alias), e.g. -v for --verbose
    /// </summary>
    public string? ShortName { get; set; }

    /// <summary>
    /// The option description (optional).
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// The option type (optional).
    /// </summary>
    public string? Type { get; set;}

    /// <summary>
    /// Whether or not the option is required (default is false).
    /// </summary>
    public bool Required { get; set; }
}