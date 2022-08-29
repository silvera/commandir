namespace Commandir.Core
{
    public interface IAction
    {
        string Name { get; }
        Task ExecuteAsync(ActionExecutionContext context);
    }
}