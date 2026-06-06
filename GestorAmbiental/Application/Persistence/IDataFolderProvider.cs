namespace GestorAmbiental.Application.Persistence;

public interface IDataFolderProvider
{
    string? DataFolderPath { get; }

    Task<string> GetDataFolderAsync(CancellationToken cancellationToken = default);
}
