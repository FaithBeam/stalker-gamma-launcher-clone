using System.Text.RegularExpressions;

namespace stalker_gamma.core.ViewModels.Tabs.Queries;

public static partial class GetAnomalyPath
{
    private static readonly string Dir = Path.GetDirectoryName(AppContext.BaseDirectory)!;

    public sealed partial class Handler
    {
        public string? Execute()
        {
            var modOrganizerIniPath = Path.Join(Dir, "..", "ModOrganizer.ini");
            if (!File.Exists(modOrganizerIniPath))
            {
                return null;
            }

            var modOrganizerIniTxt = File.ReadAllText(modOrganizerIniPath);
            return Mo2IniGamePathRx().Match(modOrganizerIniTxt).Groups[1].Value;
        }

        [GeneratedRegex(@"\r?\ngamePath=@ByteArray\((.*)\)\r?\n", RegexOptions.IgnoreCase, "en-US")]
        private static partial Regex Mo2IniGamePathRx();
    }
}
