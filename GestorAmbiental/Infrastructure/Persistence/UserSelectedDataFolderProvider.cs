using System.IO;
using GestorAmbiental.Application.Persistence;

namespace GestorAmbiental.Infrastructure.Persistence;

public sealed class UserSelectedDataFolderProvider : IDataFolderProvider
{
    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "GestorAmbiental");

    private static readonly string DataFolderSettingsPath = Path.Combine(
        SettingsDirectory,
        "data-folder.txt");

    public UserSelectedDataFolderProvider()
    {
        TryLoadSavedFolder();
    }

    public string? DataFolderPath { get; private set; }

    public void UseFolder(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            throw new ArgumentException("A pasta de dados deve ser informada.", nameof(folderPath));
        }

        DataFolderPath = Path.GetFullPath(folderPath);
        Directory.CreateDirectory(DataFolderPath);
        SaveSelectedFolder();
    }

    public Task<string> GetDataFolderAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (DataFolderPath is null)
        {
            throw new InvalidOperationException(
                "A pasta de dados precisa ser escolhida pelo usuario antes de acessar a persistencia.");
        }

        Directory.CreateDirectory(DataFolderPath);
        return Task.FromResult(DataFolderPath);
    }

    private void TryLoadSavedFolder()
    {
        try
        {
            if (!File.Exists(DataFolderSettingsPath))
            {
                return;
            }

            var savedPath = File.ReadAllText(DataFolderSettingsPath).Trim();

            if (string.IsNullOrWhiteSpace(savedPath))
            {
                return;
            }

            DataFolderPath = Path.GetFullPath(savedPath);
            Directory.CreateDirectory(DataFolderPath);
        }
        catch
        {
            DataFolderPath = null;
        }
    }

    private void SaveSelectedFolder()
    {
        try
        {
            Directory.CreateDirectory(SettingsDirectory);
            File.WriteAllText(DataFolderSettingsPath, DataFolderPath);
        }
        catch
        {
            // A pasta escolhida continua valendo nesta sessao mesmo se a preferencia nao puder ser salva.
        }
    }
}
