namespace Commandir.Core;

public sealed class CommandResult
{
    public int ReturnCode { get; set; }
    public object? ReturnValue { get; set;}

    public CommandResult(int returnCode)
        : this(returnCode, null)
    {
    }

    public CommandResult(int returnCode, object? returnValue)
    {
        ReturnCode = returnCode;
        ReturnValue = returnValue;
    }
}
