namespace Commandir.Core;

public sealed class CommandResult
{
    public CommandContext Context { get; }
    public int ReturnCode { get; }
    public object? ReturnValue { get; }

    public CommandResult(CommandContext context)
        : this(context, 0, null)
    {
    }

    public CommandResult(CommandContext context, int returnCode, object? returnValue)
    {
        Context = context;
        ReturnCode = returnCode;
        ReturnValue = returnValue;
    }
}
