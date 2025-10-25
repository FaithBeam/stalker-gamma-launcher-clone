using System.Diagnostics;

namespace stalker_gamma.core.ViewModels.Tabs.MainTab.Queries;

public interface IIsMo2VersionDowngraded
{
    public bool? Execute(IsMo2VersionDowngraded.Query q);
}

public static class IsMo2VersionDowngraded
{
    public sealed record Query(string Mo2Path);

    public sealed class Handler : IIsMo2VersionDowngraded
    {
        /// <summary>
        /// True = downgraded
        /// False = not downgraded
        /// Null = Not found
        /// </summary>
        /// <param name="q"></param>
        /// <returns></returns>
        public bool? Execute(Query q) =>
            File.Exists(q.Mo2Path)
                ? FileVersionInfo.GetVersionInfo(q.Mo2Path).FileVersion == "2.4.4"
                : null;
    }
}
