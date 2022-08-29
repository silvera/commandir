namespace Commandir.Core
{
    public class ActionExecutionContext
    {
        public IReadOnlyList<ParameterExecutionContext> Parameters { get; }

        public ActionExecutionContext(List<ParameterExecutionContext> parameters)
        {
            Parameters = parameters;            
        }
    }
}
