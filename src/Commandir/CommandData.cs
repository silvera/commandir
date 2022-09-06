namespace Commandir
{
    public class ArgumentData 
    {
        public string Name { get; }
        public string Description { get; } 

        public ArgumentData(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }

    public class OptionData 
    {
        public string Name { get; }
        public string Description { get; } 

        public bool IsRequired { get; }

        public OptionData(string name, string description, bool isRequired)
        {
            Name = name;
            Description = description;
            IsRequired = isRequired;
        }
    }

    public class CommandData
    {
        public string Name { get; }
        public string Description { get; }
 
        private readonly List<ArgumentData> _arguments = new List<ArgumentData>();
        public IReadOnlyList<ArgumentData> Arguments => _arguments;
        public void AddArgument(ArgumentData argument) => _arguments.Add(argument);

        private readonly List<OptionData> _options = new List<OptionData>();
        public IReadOnlyList<OptionData> Options => _options;
        public void AddOption(OptionData option) => _options.Add(option);

        private readonly List<ActionData> _actions = new List<ActionData>();
        public IReadOnlyList<ActionData> Actions => _actions;
        public void AddAction(ActionData action) => _actions.Add(action);

        private readonly List<CommandData> _commands = new List<CommandData>();
        public IReadOnlyList<CommandData> Commands => _commands;
        public void AddCommand(CommandData command) => _commands.Add(command);

        public CommandData(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }
}