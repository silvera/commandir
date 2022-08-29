namespace Commandir.Core
{
    public class ActionContext : Dictionary<string, string>
    {
        public string Name { get; }

        public ActionContext(string name)
        {
            Name = name;
        }
    }
}