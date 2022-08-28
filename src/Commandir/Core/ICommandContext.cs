namespace Commandir.Core
{
    public class ArgumentContext 
    {
        public string Name { get;}
        public object? Value { get;}

        public ArgumentContext(string name, object? value)
        {
            Name = name;
            Value = value;
        }
    }

    public class OptionContext 
    {
        public string Name { get;}
        public object? Value { get;}

        public OptionContext(string name, object? value)
        {
            Name = name;
            Value = value;
        }
    }

    public class HandlerContext
    {
        public ICommandContextHandler Handler { get; set;}

        public Dictionary<string, string> Entries { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    public interface ICommandContext
    {
        IReadOnlyList<ArgumentContext> Arguments { get; }
        IReadOnlyList<OptionContext> Options { get;}
    }

    public class CommandContext : ICommandContext
    {
        public IReadOnlyList<ArgumentContext> Arguments { get; }
        public IReadOnlyList<OptionContext> Options { get;}

        public CommandContext(List<ArgumentContext> arguments, List<OptionContext> options)
        {
            Arguments = arguments;
            Options = options;
        }

    }
}