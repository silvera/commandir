namespace Commandir.Core;

public interface ICommand
{
    Task ExecuteAsync();
}
