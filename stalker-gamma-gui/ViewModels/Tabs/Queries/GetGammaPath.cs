using System;
using System.IO;

namespace stalker_gamma_gui.ViewModels.Tabs.Queries;

public static class GetGammaPath
{
    private static readonly string Dir = Path.GetDirectoryName(AppContext.BaseDirectory)!;

    public sealed class Handler
    {
        public string Execute()
        {
            var gammaPath = Path.GetFullPath(Path.Join(Dir, ".."));
            return gammaPath;
        }
    }
}
