using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace stalker_gamma.core.Services;

public class FileService(Window window)
{
    public async Task<string?> SelectFolder(string title)
    {
        var directory = await _window.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions { AllowMultiple = false, Title = title }
        );
        return directory.Count > 0 ? directory[0].TryGetLocalPath() : null;
    }

    private readonly Window _window = window;
}
