using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using GestorAmbiental.Application.Persistence;
using GestorAmbiental.Domain.Common;

namespace GestorAmbiental.Infrastructure.Persistence;

public sealed class JsonFileRepository<T> : IRepository<T> where T : class, IEntity
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    private readonly IDataFolderProvider _dataFolderProvider;
    private readonly string _collectionName;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public JsonFileRepository(IDataFolderProvider dataFolderProvider, string? collectionName = null)
    {
        _dataFolderProvider = dataFolderProvider;
        _collectionName = string.IsNullOrWhiteSpace(collectionName)
            ? typeof(T).Name.ToLowerInvariant()
            : collectionName;
    }

    public async Task<IReadOnlyList<T>> ListarAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);

        try
        {
            return await CarregarAsync(cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<T?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var registros = await ListarAsync(cancellationToken);
        return registros.FirstOrDefault(registro => registro.Id == id);
    }

    public async Task<T> SalvarAsync(T entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        await _lock.WaitAsync(cancellationToken);

        try
        {
            var registros = await CarregarAsync(cancellationToken);

            if (entity.Id <= 0)
            {
                entity.Id = registros.Count == 0 ? 1 : registros.Max(registro => registro.Id) + 1;
                registros.Add(entity);
            }
            else
            {
                var index = registros.FindIndex(registro => registro.Id == entity.Id);

                if (index >= 0)
                {
                    registros[index] = entity;
                }
                else
                {
                    registros.Add(entity);
                }
            }

            await SalvarRegistrosAsync(registros, cancellationToken);
            return entity;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task ExcluirAsync(int id, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);

        try
        {
            var registros = await CarregarAsync(cancellationToken);
            registros.RemoveAll(registro => registro.Id == id);
            await SalvarRegistrosAsync(registros, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<List<T>> CarregarAsync(CancellationToken cancellationToken)
    {
        var path = await ObterArquivoAsync(cancellationToken);

        if (!File.Exists(path))
        {
            return [];
        }

        await using var stream = File.OpenRead(path);
        var registros = await JsonSerializer.DeserializeAsync<List<T>>(stream, JsonOptions, cancellationToken);
        return registros ?? [];
    }

    private async Task SalvarRegistrosAsync(List<T> registros, CancellationToken cancellationToken)
    {
        var path = await ObterArquivoAsync(cancellationToken);
        var tempPath = $"{path}.tmp";

        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, registros, JsonOptions, cancellationToken);
        }

        if (File.Exists(path))
        {
            File.Delete(path);
        }

        File.Move(tempPath, path);
    }

    private async Task<string> ObterArquivoAsync(CancellationToken cancellationToken)
    {
        var folder = await _dataFolderProvider.GetDataFolderAsync(cancellationToken);
        Directory.CreateDirectory(folder);
        return Path.Combine(folder, $"{_collectionName}.json");
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
