using ReactiveUI;

namespace stalker_gamma_gui.ViewModels.Tabs.BackupTab;

public class ModBackupVm(
    string fileName,
    long fileSize,
    string path,
    int gammaVersion,
    string gammaHash,
    string dateTime,
    string compressionMethod
) : ReactiveObject
{
    public string FileName
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = fileName;

    public long FileSize
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = fileSize;

    public string Path
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = path;

    public int GammaVersion
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = gammaVersion;

    public string GammaHash
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = gammaHash;

    public string DateTime
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = dateTime;

    public string CompressionMethod
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = compressionMethod;
}
