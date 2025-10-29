namespace stalker_gamma.core.ViewModels.Tabs.MainTab.Queries;

public interface IUserLtxSetToFullscreenWine
{
    public Task<bool?> ExecuteAsync(UserLtxSetToFullscreenWine.Query q);
}

public static class UserLtxSetToFullscreenWine
{
    public sealed record Query(string PathToUserLtx);

    public sealed class Handler : IUserLtxSetToFullscreenWine
    {
        public async Task<bool?> ExecuteAsync(Query q) =>
            File.Exists(q.PathToUserLtx)
                ? (await File.ReadAllTextAsync(q.PathToUserLtx)).Contains(
                    "rs_screenmode fullscreen"
                )
                : null;
    }
}
