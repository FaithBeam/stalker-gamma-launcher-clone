using System.Text;
using CliWrap;

namespace stalker_gamma.core.Services;

public interface ICurlService
{
    /// <summary>
    /// Whether curl service found curl-impersonate-win.exe and can execute.
    /// </summary>
    bool Ready { get; }

    Task DownloadFileAsync(
        string url,
        string pathToDownloads,
        string fileName,
        bool useCurlImpersonate,
        string? workingDir = null
    );

    Task<string> GetStringAsync(string url, string extraCmds = "", bool useCurlImpersonate = true);
}

public class CurlService(
    IHttpClientFactory clientFactory,
    IOperatingSystemService operatingSystemService
) : ICurlService
{
    private HttpClient? _httpClient = clientFactory.CreateClient();
    private static readonly string Dir = Path.GetDirectoryName(AppContext.BaseDirectory)!;
    private readonly IOperatingSystemService _operatingSystemService = operatingSystemService;

    /// <summary>
    /// Whether curl service found curl-impersonate-win.exe and can execute.
    /// </summary>
    public bool Ready { get; private set; } =
        File.Exists(Path.Join(PathToCurlImpersonateWin, "Curl.exe"));

    public async Task DownloadFileAsync(
        string url,
        string pathToDownloads,
        string fileName,
        bool useCurlImpersonate,
        string? workingDir = null
    )
    {
        if (useCurlImpersonate)
        {
            var stdOut = new StringBuilder();
            var stdErr = new StringBuilder();
            var cmd = Cli.Wrap(Path.Join(PathToCurlImpersonateWin, "curl.exe"))
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOut))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErr));
            if (!string.IsNullOrWhiteSpace(workingDir))
            {
                cmd = cmd.WithWorkingDirectory(workingDir);
            }
            if (_operatingSystemService.IsWindows())
            {
                cmd = cmd.WithArguments(
                    $"--config \"{Path.Join(PathToCurlImpersonateWin, "config", "chrome116.config")}\" --header \"@{Path.Join(PathToCurlImpersonateWin, "config", "chrome116.header")}\" -Lo \"{Path.Join(pathToDownloads, fileName)}\" {url}"
                );
            }
            else if (_operatingSystemService.IsMacOS() || _operatingSystemService.IsLinux())
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

    public async Task<string> GetStringAsync(
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
        Dir,
        "resources",
        "curl-impersonate-win"
    );

    private const string MacosGetStringCmd =
        "docker run --rm lwthiker/curl-impersonate:0.6-chrome curl_chrome116";
    private static readonly string WindowsGetStringCmd =
        $"--config \"{Path.Join(PathToCurlImpersonateWin, "config", "chrome116.config")}\" --header \"@{Path.Join(PathToCurlImpersonateWin, "config", "chrome116.header")}\"";
    private const string LinuxGetStringCmd =
        "docker run --rm lwthiker/curl-impersonate:0.6-chrome curl_chrome116";
    private string GetStringCmd =>
        _operatingSystemService.IsWindows() ? WindowsGetStringCmd
        : _operatingSystemService.IsMacOS() ? MacosGetStringCmd
        : LinuxGetStringCmd;
}
