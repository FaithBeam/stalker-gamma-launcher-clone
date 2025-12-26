using ReactiveUI;

namespace stalker_gamma_gui.ViewModels.Services;

public class ModalService : ReactiveObject
{
    public bool ModalOpen
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? ModalTitle
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? ModalText
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public void ShowInformationDlg(string content)
    {
        ModalTitle = "Information";
        ModalText = content;
        ModalOpen = true;
    }

    public void ShowErrorDlg(string content)
    {
        ModalTitle = "Error";
        ModalText = content;
        ModalOpen = true;
    }
}
