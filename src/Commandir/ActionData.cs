namespace Commandir
{
    public class ActionData : Dictionary<string, object?>
    {
        public string Name { get; }

        public ActionData(string name)
        {
            Name = name;
        }
    }
}