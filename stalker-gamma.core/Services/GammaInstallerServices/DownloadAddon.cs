using System.Collections.Immutable;
using System.Text.RegularExpressions;
using stalker_gamma.core.Models;
using stalker_gamma.core.Utilities;

namespace stalker_gamma.core.Services.GammaInstallerServices;

public partial class DownloadAddon(ModDb modDb)
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="record"></param>
    /// <param name="destination">Can either be a path to a folder or a path to a file archive</param>
    /// <param name="checkMd5Pct"></param>
    /// <param name="downloadPct"></param>
    /// <param name="invalidateMirrorCache"></param>
    public async Task DownloadAsync(
        ModListRecord record,
        string destination,
        Action<double> checkMd5Pct,
        Action<double> downloadPct,
        bool invalidateMirrorCache = false
    )
    {
        if (record is ModDbRecord)
        {
            if (
                Path.Exists(destination)
                    && !string.IsNullOrWhiteSpace(record.Md5ModDb)
                    && await Md5Utility.CalculateFileMd5Async(destination, checkMd5Pct)
                        != record.Md5ModDb
                || !Path.Exists(destination)
            )
            {
                await modDb.GetModDbLinkCurl(
                    record.DlLink!,
                    destination,
                    downloadPct,
                    invalidateMirrorCache: invalidateMirrorCache
                );
            }
        }
        else
        {
            var profileAndRepoMatch = GithubRx().Match(record.DlLink!);
            var (profile, repo) = (
                profileAndRepoMatch.Groups["profile"].Value,
                profileAndRepoMatch.Groups["repo"].Value
            );
            var matchedRef = Rxs.First(rx => rx(record.DlLink!).Success)(record.DlLink!)
                .Groups["ref"]
                .Value;

            if (Directory.Exists(destination))
            {
                // delete git dir because attempting to update repos on different refs is a pita. Should figure this out later
                DirUtils.NormalizePermissions(destination);
                Directory.Delete(destination, true);
            }

            await GitUtility.CloneGitRepo(
                destination,
                string.Format(GithubUrl, profile, repo),
                onProgress: downloadPct
            );

            if (matchedRef != "latest")
            {
                await GitUtility.CheckoutBranch(destination, matchedRef);
            }
        }
    }

    private static readonly ImmutableList<Func<string, Match>> Rxs =
    [
        txt => Rx1().Match(txt),
        txt => Rx2().Match(txt),
        txt => ShaRx().Match(txt),
    ];

    [GeneratedRegex("releases/download/(?<ref>.*)/.*?")]
    private static partial Regex Rx1();

    [GeneratedRegex("refs/(?<refType>tags|heads)/(?<ref>.+)\\..+")]
    private static partial Regex Rx2();

    [GeneratedRegex("(?<ref>[a-fA-F0-9]{40})")]
    private static partial Regex ShaRx();

    [GeneratedRegex(
        @"https://github.com/(?<profile>[\w\-_]*)/+?(?<repo>[\w\-_\.]*).*/(?<archive>.*)\.+?"
    )]
    private static partial Regex GithubRx();

    private const string GithubUrl = "https://github.com/{0}/{1}";
}
