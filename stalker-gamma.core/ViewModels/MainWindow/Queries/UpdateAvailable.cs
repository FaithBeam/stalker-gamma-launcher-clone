using System.Text.Json;
using stalker_gamma.core.Services;
using stalker_gamma.core.ViewModels.MainWindow.Models;

namespace stalker_gamma.core.ViewModels.MainWindow.Queries;

public static class UpdateAvailable
{
    public sealed record Response(
        bool IsUpdateAvailable,
        string CurrentVersion,
        string LatestVersion,
        string ChangeNotes,
        string Link,
        string DownloadLink
    );

    public sealed class Handler(IHttpClientFactory hcf, IVersionService versionService)
    {
        private const string StalkerGammaRepoRoute =
            "https://api.github.com/repos/FaithBeam/stalker-gamma-launcher-clone/releases/latest";
        private readonly IHttpClientFactory _hcf = hcf;
        private readonly IVersionService _versionService = versionService;

        public async Task<Response> ExecuteAsync()
        {
            var client = _hcf.CreateClient("githubDlArchive");
            var response = await client.GetAsync(StalkerGammaRepoRoute);
            if (!response.IsSuccessStatusCode)
            {
                throw new UpdateAvailableException("Failed to get latest release from Github");
            }

            var deserialized = await JsonSerializer.DeserializeAsync(
                await response.Content.ReadAsStreamAsync(),
                jsonTypeInfo: GitHubReleaseCtx.Default.GitHubRelease
            );
            if (
                deserialized is null
                || string.IsNullOrWhiteSpace(deserialized.TagName)
                || !Version.TryParse(_versionService.GetVersion(), out var runningVer)
                || !Version.TryParse(deserialized.TagName, out var latestVer)
                || string.IsNullOrWhiteSpace(deserialized.Body)
                || string.IsNullOrWhiteSpace(deserialized.HtmlUrl)
                || string.IsNullOrWhiteSpace(
                    deserialized.Assets?.FirstOrDefault()?.BrowserDownloadUrl
                )
            )
            {
                throw new UpdateAvailableException("Failed to get latest release from Github");
            }

            return runningVer.CompareTo(latestVer) switch
            {
                -1 => new Response(
                    true,
                    runningVer.ToString(),
                    latestVer.ToString(),
                    deserialized.Body,
                    deserialized.HtmlUrl,
                    deserialized.Assets.First().BrowserDownloadUrl!
                ),
                0 or 1 => new Response(
                    false,
                    runningVer.ToString(),
                    latestVer.ToString(),
                    deserialized.Body,
                    deserialized.HtmlUrl,
                    deserialized.Assets.First().BrowserDownloadUrl!
                ),
                _ => throw new ArgumentOutOfRangeException(),
            };
        }
    }
}

public class UpdateAvailableException(string msg) : Exception(msg);
