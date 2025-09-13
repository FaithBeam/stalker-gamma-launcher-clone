using System.Reactive.Linq;
using System.Text.RegularExpressions;
using CliWrap.EventStream;
using stalker_gamma.core.Utilities;

namespace stalker_gamma.core.ViewModels.Tabs.BackupTab.Queries;

public static partial class GetArchiveCompressionMethod
{
    public sealed record Query(string PathToArchive);

    public sealed partial class Handler
    {
        public async Task<string?> ExecuteAsync(Query q)
        {
            using var cts = new CancellationTokenSource();
            string? method = null;
            try
            {
                await ArchiveUtility
                    .List(q.PathToArchive, cts.Token)
                    .ForEachAsync(
                        cmdEvt =>
                        {
                            switch (cmdEvt)
                            {
                                case StandardOutputCommandEvent stdOut:
                                    var match = MethodRx().Match(stdOut.Text);
                                    if (match.Success)
                                    {
                                        method = match.Groups["method"].Value;
                                        cts.Cancel();
                                    }

                                    break;
                            }
                        },
                        cts.Token
                    );
            }
            catch (OperationCanceledException)
            {
                return method;
            }

            return method;
        }

        [GeneratedRegex("Method = (?<method>.+)")]
        private static partial Regex MethodRx();
    }
}
