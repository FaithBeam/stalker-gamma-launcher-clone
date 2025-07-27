using ReactiveUI;

namespace stalker_gamma.core.Services;

public class IsBusyService : ReactiveObject
{
    private bool _isBusy;

    public bool IsBusy
    {
        get => _isBusy;
        set => this.RaiseAndSetIfChanged(ref _isBusy, value);
    }
}
