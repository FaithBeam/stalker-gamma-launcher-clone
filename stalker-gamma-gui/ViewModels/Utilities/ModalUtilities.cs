using System;

namespace stalker_gamma.core.ViewModels.Utilities;

public static class ModalUtilities
{
    public static void ShowInformationDlg(
        ref bool modalShown,
        ref string modalTitle,
        Action modalContent
    )
    {
        modalTitle = "Info";
        modalContent();
        modalShown = true;
    }

    public static void ShowErrorDlg(ref bool modalShown, ref string modalTitle, Action modalContent)
    {
        modalTitle = "Error";
        modalContent();
        modalShown = true;
    }
}
