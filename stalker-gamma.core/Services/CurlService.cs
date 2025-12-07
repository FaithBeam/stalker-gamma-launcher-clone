using System.Reactive.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CliWrap;
using CliWrap.Builders;
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

    Task<string> GetStringAsync(string url);

    Task DownloadFileAsync(
        string url,
        string pathToDownloads,
        string fileName,
        Action<double>? onProgress = null,
        string? workingDir = null
    );
}

public partial class CurlService : ICurlService
{
    private static readonly string Dir = Path.GetDirectoryName(AppContext.BaseDirectory)!;

    /// <summary>
    /// Whether curl service found curl-impersonate-win.exe and can execute.
    /// </summary>
    public bool Ready { get; private set; } = File.Exists(PathToCurlImpersonate);

    public async Task DownloadFileAsync(
        string url,
        string pathToDownloads,
        string fileName,
        Action<double>? onProgress = null,
        string? workingDir = null
    )
    {
        var cmd = Cli.Wrap(PathToCurlImpersonate);
        if (!string.IsNullOrWhiteSpace(workingDir))
        {
            cmd = cmd.WithWorkingDirectory(workingDir);
        }

        cmd = cmd.WithArguments(argBuilder =>
            argBuilder
                .Add("--progress-bar")
                .Add("--clobber")
                .Add("-Lo")
                .Add(Path.Join(pathToDownloads, fileName))
                .Add(url)
                .AddImpersonation()
        );
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
                                onProgress?.Invoke(parsed);
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
                Args: {string.Join(" ", cmd.Arguments)}
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
        var stdOut = new StringBuilder();
        var stdErr = new StringBuilder();
        var cmd = Cli.Wrap(PathToCurlImpersonate)
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOut))
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErr));
        if (!string.IsNullOrWhiteSpace(workingDir))
        {
            cmd = cmd.WithWorkingDirectory(workingDir);
        }

        cmd = cmd.WithArguments(argBuilder =>
            argBuilder
                .Add("--clobber")
                .Add("-Lo")
                .Add(Path.Join(pathToDownloads, fileName))
                .Add(url)
                .AddImpersonation()
        );
        try
        {
            await cmd.ExecuteAsync();
        }
        catch (CommandExecutionException e)
        {
            throw new CurlDownloadException(
                $"""
                Error downloading file: {url}
                Args: {string.Join(" ", cmd.Arguments)}
                StdOut: {stdOut}
                StdErr: {stdErr}
                """,
                e
            );
        }
    }

    public async Task<string> GetStringAsync(string url)
    {
        var stdOut = new StringBuilder();
        var stdErr = new StringBuilder();
        try
        {
            var cmd = Cli.Wrap(PathToCurlImpersonate)
                .WithArguments(argBuilder =>
                    argBuilder.Add("--no-progress-meter").Add(url).AddImpersonation()
                )
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOut))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErr));
            var result = await cmd.ExecuteAsync();
            return stdOut.ToString();
        }
        catch (CommandExecutionException e)
        {
            throw new CurlDownloadException($"Error getting string from url: {url}", e);
        }
    }

    private static readonly string OsCurlName = OperatingSystem.IsWindows()
        ? "curl.exe"
        : "curl-impersonate";
    private static readonly string OsName =
        OperatingSystem.IsWindows() ? "win"
        : OperatingSystem.IsLinux() ? "linux"
        : "mac";
    private static readonly string PathToCurlImpersonate = Path.Join(
        Dir,
        "resources",
        "curl-impersonate",
        OsName,
        OsCurlName
    );

    [GeneratedRegex(@"(\d+(\.\d+)?)\s*%", RegexOptions.Compiled)]
    private static partial Regex CurlProgressRx();
}

public class CurlDownloadException : Exception
{
    public CurlDownloadException(string message, Exception innerException)
        : base(message, innerException) { }
}

public static class ArgumentsBuilderExtensions
{
    public static ArgumentsBuilder AddImpersonation(this ArgumentsBuilder argBuilder) =>
        argBuilder
            .Add("--ciphers")
            .Add(
                "TLS_AES_128_GCM_SHA256:TLS_AES_256_GCM_SHA384:TLS_CHACHA20_POLY1305_SHA256:ECDHE-ECDSA-AES128-GCM-SHA256:ECDHE-RSA-AES128-GCM-SHA256:ECDHE-ECDSA-AES256-GCM-SHA384:ECDHE-RSA-AES256-GCM-SHA384:ECDHE-ECDSA-CHACHA20-POLY1305:ECDHE-RSA-CHACHA20-POLY1305:ECDHE-RSA-AES128-SHA:ECDHE-RSA-AES256-SHA:AES128-GCM-SHA256:AES256-GCM-SHA384:AES128-SHA:AES256-SHA"
            )
            .Add("--curves")
            .Add("X25519MLKEM768:X25519:P-256:P-384")
            .Add("-H")
            .Add(
                "sec-ch-ua: \"Chromium\";v=\"136\", \"Google Chrome\";v=\"136\", \"Not(A:Brand\";v=\"99\""
            )
            .Add("-H")
            .Add("sec-ch-ua-mobile: ?0")
            .Add("-H")
            .Add("sec-ch-ua-platform: \"macOS\"")
            .Add("-H")
            .Add("Upgrade-Insecure-Requests: 1")
            .Add("-H")
            .Add(
                "User-Agent: Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/136.0.0.0 Safari/537.36"
            )
            .Add("-H")
            .Add(
                "Accept: text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7"
            )
            .Add("-H")
            .Add("Sec-Fetch-Site: none")
            .Add("-H")
            .Add("Sec-Fetch-Mode: navigate")
            .Add("-H")
            .Add("Sec-Fetch-User: ?1")
            .Add("-H")
            .Add("Sec-Fetch-Dest: document")
            .Add("-H")
            .Add("Accept-Encoding: gzip, deflate, br, zstd")
            .Add("-H")
            .Add("Accept-Language: en-US,en;q=0.9")
            .Add("-H")
            .Add("Priority: u=0, i")
            .Add("--http2")
            .Add("--http2-settings")
            .Add("1:65536;2:0;4:6291456;6:262144")
            .Add("--http2-window-update")
            .Add("15663105")
            .Add("--http2-stream-weight")
            .Add("256")
            .Add("--http2-stream-exclusive")
            .Add("1")
            .Add("--compressed")
            .Add("--ech")
            .Add("grease")
            .Add("--tlsv1.2")
            .Add("--alps")
            .Add("--tls-permute-extensions")
            .Add("--cert-compression")
            .Add("brotli")
            .Add("--tls-grease")
            .Add("--tls-use-new-alps-codepoint")
            .Add("--tls-signed-cert-timestamps");
}
