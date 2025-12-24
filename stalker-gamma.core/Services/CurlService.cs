using System.Text;
using System.Text.RegularExpressions;
using CliWrap;
using CliWrap.Builders;
using stalker_gamma.core.Models;

namespace stalker_gamma.core.Services;

public interface ICurlService
{
    /// <summary>
    /// Whether curl service found curl-impersonate-win.exe and can execute.
    /// </summary>
    bool Ready { get; }

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
    ) =>
        await ExecuteCurlCmdAsync(
            ["--progress-bar", "--clobber", "-Lo", Path.Join(pathToDownloads, fileName), url],
            workingDir: workingDir,
            onProgress: onProgress
        );

    public async Task<string> GetStringAsync(string url) =>
        (await ExecuteCurlCmdAsync(["--no-progress-meter", url])).StdOut;

    private static async Task<StdOutStdErrOutput> ExecuteCurlCmdAsync(
        string[] args,
        Action<double>? onProgress = null,
        Action<string>? txtProgress = null,
        string? workingDir = null,
        CancellationToken? cancellationToken = null
    )
    {
        var stdOut = new StringBuilder();
        var stdErr = new StringBuilder();

        List<string> finalArgs = [.. args];
        if (OperatingSystem.IsWindows())
        {
            finalArgs.AddRange("--cacert", Path.Join("resources", "cacert.pem"));
        }

        try
        {
            await Cli.Wrap(PathToCurlImpersonate)
                .WithArguments(argBuilder => argBuilder.Add(finalArgs).AddImpersonation())
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOut))
                .WithStandardErrorPipe(
                    PipeTarget.Merge(
                        PipeTarget.ToStringBuilder(stdErr),
                        PipeTarget.ToDelegate(line =>
                        {
                            txtProgress?.Invoke(line);
                            if (
                                onProgress is not null
                                && ProgressRx().IsMatch(line)
                                && double.TryParse(
                                    ProgressRx().Match(line).Groups[1].Value,
                                    out var parsed
                                )
                            )
                            {
                                onProgress(parsed / 100);
                            }
                        })
                    )
                )
                .WithWorkingDirectory(workingDir ?? "")
                .ExecuteAsync(cancellationToken ?? CancellationToken.None);
        }
        catch (Exception e)
        {
            throw new CurlServiceException(
                $"""
                Error executing curl command
                {string.Join(' ', args)}
                StdOut: {stdOut}
                StdErr: {stdErr}
                Exception: {e}
                """,
                e
            );
        }

        return new StdOutStdErrOutput(stdOut.ToString(), stdErr.ToString());
    }

    private static readonly string PathToCurlImpersonate = Path.Join(
        Dir,
        "resources",
        OperatingSystem.IsWindows() ? "curl.exe" : "curl-impersonate"
    );

    [GeneratedRegex(@"(\d+(\.\d+)?)\s*%", RegexOptions.Compiled)]
    private static partial Regex ProgressRx();
}

public class CurlServiceException(string message, Exception innerException)
    : Exception(message, innerException);

internal static class ArgumentsBuilderExtensions
{
    internal static ArgumentsBuilder AddImpersonation(this ArgumentsBuilder argBuilder) =>
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
