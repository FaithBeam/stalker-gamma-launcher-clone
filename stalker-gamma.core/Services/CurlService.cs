using System.Reactive.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CliWrap;
using CliWrap.EventStream;
using CliWrap.Exceptions;

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

    Task DownloadFileAsync(
        string url,
        string pathToDownloads,
        string fileName,
        IProgress<double> progress,
        string? workingDir = null
    );
}

public partial class CurlService(
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
        IProgress<double> progress,
        string? workingDir = null
    )
    {
        var cmd = Cli.Wrap(OsShell);
        if (!string.IsNullOrWhiteSpace(workingDir))
        {
            cmd = cmd.WithWorkingDirectory(workingDir);
        }

        var args =
            $"{OsScriptPath} --progress-bar --clobber -Lo {Path.Join(pathToDownloads, fileName).Replace(" ", "^ ")} {url}";
        cmd = cmd.WithArguments(argBuilder => argBuilder.Add(OsShellArgs).Add(args));
        try
        {
            await cmd.Observe()
                .ForEachAsync(onNext =>
                {
                    switch (onNext)
                    {
                        case StandardErrorCommandEvent stdErr:
                            if (
                                CurlProgressRx().IsMatch(stdErr.Text)
                                && double.TryParse(
                                    CurlProgressRx().Match(stdErr.Text).Groups[1].Value,
                                    out var parsed
                                )
                            )
                            {
                                progress.Report(parsed);
                            }
                            break;
                        case ExitedCommandEvent:
                        case StandardOutputCommandEvent:
                        case StartedCommandEvent:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(onNext));
                    }
                });
        }
        catch (CommandExecutionException e)
        {
            throw new CurlDownloadException(
                $"""
                Error downloading file: {url}
                Args: {args}
                """,
                e
            );
        }
    }

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
            var cmd = Cli.Wrap(OsShell)
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOut))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErr));
            if (!string.IsNullOrWhiteSpace(workingDir))
            {
                cmd = cmd.WithWorkingDirectory(workingDir);
            }

            var args =
                $"{OsScriptPath} --clobber -Lo {Path.Join(pathToDownloads, fileName).Replace(" ", "^ ")} {url}";
            cmd = cmd.WithArguments(argBuilder => argBuilder.Add(OsShellArgs).Add(args));
            try
            {
                await cmd.ExecuteAsync();
            }
            catch (CommandExecutionException e)
            {
                throw new CurlDownloadException(
                    $"""
                    Error downloading file: {url}
                    Args: {args}
                    StdOut: {stdOut}
                    StdErr: {stdErr}
                    """,
                    e
                );
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
            try
            {
                var cmd = Cli.Wrap(OsShell)
                    .WithArguments(argBuilder =>
                        argBuilder.Add(OsShellArgs).Add($"{OsScriptPath} --no-progress-meter {url}")
                    )
                    .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOut))
                    .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErr));
                var result = await cmd.ExecuteAsync();
                return stdOut.ToString();
            }
            catch (Exception e)
            {
                Console.WriteLine(
                    $"""
                    {stdOut}
                    {stdErr}
                    {e}
                    """
                );
                throw;
            }
        }

        _httpClient ??= new HttpClient();
        var response = await _httpClient.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();
        return content;
    }

    private static readonly string PathToCurlImpersonateWin = Path.Join(
        Dir,
        "resources",
        "curl-impersonate",
        "win"
    );
    private static readonly string OsShell = OperatingSystem.IsWindows() ? "cmd" : "bash";
    private static readonly string OsShellArgs = OperatingSystem.IsWindows() ? "/c" : "-c";
    private static readonly string OsScriptName = OperatingSystem.IsWindows()
        ? "curl_chrome136.bat"
        : "curl_chrome136";
    private static readonly string OsScriptPath = Path.Join(PathToCurlImpersonateWin, OsScriptName)
        .Replace(" ", "^ ");

    [GeneratedRegex(@"(\d+(\.\d+)?)\s*%", RegexOptions.Compiled)]
    private static partial Regex CurlProgressRx();
}

public class CurlDownloadException : Exception
{
    public CurlDownloadException(string message, Exception innerException)
        : base(message, innerException) { }
}
