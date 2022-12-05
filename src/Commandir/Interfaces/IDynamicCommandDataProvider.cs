namespace Commandir.Interfaces;

public interface IDynamicCommandData
{
    string? Path { get; }
    Dictionary<string, object?>? Parameters { get; }
}

public interface IDynamicCommandDataProvider
{
    IDynamicCommandData GetCommandData();
}

public interface ICancellationTokenProvider
{
    CancellationToken GetCancellationToken();
}