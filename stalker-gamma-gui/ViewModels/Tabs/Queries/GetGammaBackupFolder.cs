using System;
using System.IO;
using stalker_gamma.core.Models;

namespace stalker_gamma_gui.ViewModels.Tabs.Queries;

public static class GetGammaBackupFolder
{
    public sealed class Handler(GlobalSettings gs)
    {
        public string Execute() =>
            string.IsNullOrWhiteSpace(gs.GammaBackupPath)
                ? Path.Join(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "GAMMA"
                )
                : gs.GammaBackupPath;
    }
}
