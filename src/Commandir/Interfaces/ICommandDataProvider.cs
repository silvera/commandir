namespace Commandir.Interfaces;

public interface ICommandData
{
    string? Type { get; }
    Dictionary<string, object?>? Parameters { get; }
}

public interface ICommandDataProvider<TCommandData> where TCommandData : ICommandData
{
    TCommandData? GetRootCommandData();
    TCommandData? GetCommandData(string path);
}