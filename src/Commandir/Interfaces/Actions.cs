namespace Commandir.Interfaces;


public sealed class ActionRequest
{
    public IServiceProvider Services { get; }
    public CancellationToken CancellationToken { get; }

    public ActionRequest(IServiceProvider services, CancellationToken cancellationToken)
    {
        Services = services;
        CancellationToken = cancellationToken;
    }
}

public sealed class ActionResponse
{
    public object? Value { get; set; }
}

public interface IActionHandler
{
    Task<ActionResponse> HandleAsync(ActionRequest request);
}

public interface IActionHandlerProvider
{
    IActionHandler? GetAction(string actionName);
}