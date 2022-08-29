namespace Commandir.Core
{
    public interface IActionHandler
    {
        string Name { get; }
        Task ExecuteAsync(ActionExecutionContext context);
    }
}