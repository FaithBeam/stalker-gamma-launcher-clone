using ReactiveUI;

namespace stalker_gamma.core.Services;

public interface IIsBusyService
{
    bool IsBusy { get; set; }
}

public class IsBusyService : ReactiveObject, IIsBusyService
{
    private bool _isBusy;

    public bool IsBusy
    {
        get => _isBusy;
        set => this.RaiseAndSetIfChanged(ref _isBusy, value);
    }
}
