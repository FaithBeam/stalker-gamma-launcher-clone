using System.Text;
using CliWrap;

namespace stalker_gamma.core.Utilities;

public static class Curl
{
    private static HttpClient? _httpClient;
    private static string _dir = Path.GetDirectoryName(AppContext.BaseDirectory)!;

    public static async Task DownloadFileAsync(
        string url,
        string pathToDownloads,
        string fileName,
        bool useCurlImpersonate
    )
    {
        if (useCurlImpersonate)
        {
            var stdOut = new StringBuilder();
            var stdErr = new StringBuilder();
            var cmd = Cli.Wrap(Path.Join(PathToCurlImpersonateWin, "curl.exe"))
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOut))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErr));
            if (OperatingSystem.IsWindows())
            {
                cmd = cmd.WithArguments(
                    $"--config {Path.Join(PathToCurlImpersonateWin, "config", "chrome116.config")} --header @{Path.Join(PathToCurlImpersonateWin, "config", "chrome116.header")} -Lo \"{Path.Join(pathToDownloads, fileName)}\" {url}"
                );
            }
            else if (OperatingSystem.IsMacOS() || OperatingSystem.IsLinux())
            {
                cmd = cmd.WithArguments(
                    $"docker run --mount type=bind,src={pathToDownloads},dst=/downloads --rm lwthiker/curl-impersonate:0.6-chrome curl_chrome116 -Lo /downloads/{fileName} {url}"
                );
            }
            else
            {
                throw new Exception("Unsupported OS");
            }
            var result = await cmd.ExecuteAsync();
            if (!result.IsSuccess)
            {
                throw new Exception($"{stdErr}\n{stdOut}");
            }
        }
        else
        {
            _httpClient ??= new HttpClient();
            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStreamAsync();
            await using var fs = File.Create(Path.Join(pathToDownloads, fileName));
            await content.CopyToAsync(fs);
        }
    }

    public static async Task<string> GetStringAsync(
        string url,
        string extraCmds = "",
        bool useCurlImpersonate = true
    )
    {
        if (useCurlImpersonate)
        {
            var stdOut = new StringBuilder();
            var stdErr = new StringBuilder();
            var cmd = Cli.Wrap(Path.Join(PathToCurlImpersonateWin, "curl.exe"))
                .WithArguments($"{GetStringCmd} {CommonCurlArgs} {url} {extraCmds}")
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOut))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErr));
            var result = await cmd.ExecuteAsync();
            return stdOut.ToString();
        }

        _httpClient ??= new HttpClient();
        var response = await _httpClient.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();
        return content;
    }

    private const string CommonCurlArgs = "--no-progress-meter";

    private static readonly string PathToCurlImpersonateWin = Path.Join(
        _dir,
        "resources",
        "curl-impersonate-win"
    );

    private const string MacosGetStringCmd =
        "docker run --rm lwthiker/curl-impersonate:0.6-chrome curl_chrome116";
    private static readonly string WindowsGetStringCmd =
        $"--config \"{Path.Join(PathToCurlImpersonateWin, "config", "chrome116.config")}\" --header \"@{Path.Join(PathToCurlImpersonateWin, "config", "chrome116.header")}\"";
    private const string LinuxGetStringCmd =
        "docker run --rm lwthiker/curl-impersonate:0.6-chrome curl_chrome116";
    private static readonly string GetStringCmd =
        OperatingSystem.IsWindows() ? WindowsGetStringCmd
        : OperatingSystem.IsMacOS() ? MacosGetStringCmd
        : LinuxGetStringCmd;
}
