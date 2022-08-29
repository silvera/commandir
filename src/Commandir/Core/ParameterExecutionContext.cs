namespace Commandir.Core
{
    public class ParameterExecutionContext
    {
        public string Name { get; }
        public object? Value { get; }

        public ParameterExecutionContext(string name, object? value)
        {
            Name = name;
            Value = value;
        }
    }
}