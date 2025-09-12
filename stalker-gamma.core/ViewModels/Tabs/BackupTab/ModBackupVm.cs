using ReactiveUI;

namespace stalker_gamma.core.ViewModels.Tabs.BackupTab;

public class ModBackupVm(string fileName, long fileSize, string path, int gammaVersion)
    : ReactiveObject
{
    private string _fileName = fileName;
    private long _fileSize = fileSize;
    private string _path = path;
    private int _gammaVersion = gammaVersion;

    public string FileName
    {
        get => _fileName;
        set => this.RaiseAndSetIfChanged(ref _fileName, value);
    }

    public long FileSize
    {
        get => _fileSize;
        set => this.RaiseAndSetIfChanged(ref _fileSize, value);
    }

    public string Path
    {
        get => _path;
        set => this.RaiseAndSetIfChanged(ref _path, value);
    }

    public int GammaVersion
    {
        get => _gammaVersion;
        set => this.RaiseAndSetIfChanged(ref _gammaVersion, value);
    }
}
