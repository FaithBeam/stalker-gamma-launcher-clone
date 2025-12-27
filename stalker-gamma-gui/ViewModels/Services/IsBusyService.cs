using ReactiveUI;

namespace stalker_gamma.core.Services;

public interface IIsBusyService
{
    bool IsBusy { get; set; }
}

public class IsBusyService : ReactiveObject, IIsBusyService
{
    public bool IsBusy
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }
}
