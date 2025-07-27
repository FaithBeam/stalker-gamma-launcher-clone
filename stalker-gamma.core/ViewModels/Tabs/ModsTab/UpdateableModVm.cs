using ReactiveUI;

namespace stalker_gamma.core.ViewModels.Tabs.ModsTab;

public class UpdateableModVm(
    string addonName,
    string? localMd5,
    string? remoteMd5,
    string url,
    string archiveName
) : ViewModelBase
{
    private string _addonName = addonName;
    private string? _localMd5 = localMd5;
    private string? _remoteMd5 = remoteMd5;
    private string _url = url;
    private string _archiveName = archiveName;

    public string AddonName
    {
        get => _addonName;
        set => this.RaiseAndSetIfChanged(ref _addonName, value);
    }

    public string? LocalMd5
    {
        get => _localMd5;
        set => this.RaiseAndSetIfChanged(ref _localMd5, value);
    }

    public string? RemoteMd5
    {
        get => _remoteMd5;
        set => this.RaiseAndSetIfChanged(ref _remoteMd5, value);
    }

    public string Url
    {
        get => _url;
        set => this.RaiseAndSetIfChanged(ref _url, value);
    }

    public string ArchiveName
    {
        get => _archiveName;
        set => this.RaiseAndSetIfChanged(ref _archiveName, value);
    }
}
