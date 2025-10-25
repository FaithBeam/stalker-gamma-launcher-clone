namespace stalker_gamma.core.ViewModels.Tabs.MainTab.Queries;

public interface IGetLocalGammaVersion
{
    public Task<string?> ExecuteAsync(GetLocalGammaVersion.Query q);
}

public static class GetLocalGammaVersion
{
    public sealed record Query(string PathToVersionTxt);

    public sealed class Handler : IGetLocalGammaVersion
    {
        public async Task<string?> ExecuteAsync(Query q) =>
            File.Exists(q.PathToVersionTxt)
                ? (await File.ReadAllTextAsync(q.PathToVersionTxt)).Trim()
                : null;
    }
}
