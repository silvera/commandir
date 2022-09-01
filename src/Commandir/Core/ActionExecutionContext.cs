namespace Commandir.Core
{
    public interface IActionExecutionContext
    {
        IReadOnlyList<ParameterExecutionContext> Parameters { get; }
    }

    public class ActionExecutionContext : IActionExecutionContext
    {
        public IReadOnlyList<ParameterExecutionContext> Parameters { get; }

        public ActionExecutionContext(List<ParameterExecutionContext> parameters)
        {
            Parameters = parameters;            
        }
    }
}
