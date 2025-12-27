using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ReactiveUI;
using stalker_gamma.core.Models;
using stalker_gamma.core.Utilities;

namespace stalker_gamma.core.Services;

public class SettingsFileService : ReactiveObject
{
    public SettingsFile SettingsFile { get; }

    public bool SettingsInitialized
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public SettingsFileService()
    {
        try
        {
            if (!Directory.Exists(SettingsDirectory))
            {
                Directory.CreateDirectory(SettingsDirectory);
            }

            if (!File.Exists(SettingsFilePath))
            {
                File.WriteAllText(
                    SettingsFilePath,
                    JsonSerializer.Serialize(
                        new SettingsFile(),
                        jsonTypeInfo: SettingsFileCtx.Default.SettingsFile
                    )
                );
            }

            SettingsFile =
                JsonSerializer.Deserialize(
                    File.ReadAllText(SettingsFilePath),
                    SettingsFileCtx.Default.SettingsFile
                )
                ?? throw new SettingsFileServiceException(
                    $"Failed to deserialize settings file: {SettingsFilePath}"
                );

            this.WhenAnyValue(x => x.SettingsInitialized, selector: x => x)
                .Subscribe(_ =>
                {
                    EnsureFolderCreated(SettingsFile.BaseGammaDirectory!);
                    EnsureFolderCreated(SettingsFile.AnomalyDir!);
                    EnsureFolderCreated(SettingsFile.GammaDir!);
                    EnsureFolderCreated(SettingsFile.CacheDir!);
                    CreateSymbolicLinkUtility.Create(
                        Path.Join(SettingsFile.GammaDir, "downloads"),
                        SettingsFile.CacheDir!
                    );
                });

            if (!string.IsNullOrWhiteSpace(SettingsFile.BaseGammaDirectory))
            {
                SettingsInitialized = true;
            }
        }
        catch (Exception e)
        {
            throw new SettingsFileServiceException("Error creating settings file", e);
        }
    }

    public async Task SaveAsync()
    {
        try
        {
            await File.WriteAllTextAsync(
                SettingsFilePath,
                JsonSerializer.Serialize(
                    SettingsFile,
                    jsonTypeInfo: SettingsFileCtx.Default.SettingsFile
                )
            );
        }
        catch (Exception e)
        {
            throw new SettingsFileServiceException(
                $"Error saving settings file: {SettingsFilePath}",
                e
            );
        }
    }

    private static void EnsureFolderCreated(string path)
    {
        if (!string.IsNullOrWhiteSpace(path) && !Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "GAMMA"
    );

    private static readonly string SettingsFilePath = Path.Combine(
        SettingsDirectory,
        "settings.json"
    );
}

public class SettingsFileServiceException : Exception
{
    public SettingsFileServiceException(string msg)
        : base(msg) { }

    public SettingsFileServiceException(string msg, Exception ex)
        : base(msg, ex) { }
}
