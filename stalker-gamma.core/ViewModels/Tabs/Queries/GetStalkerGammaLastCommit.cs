using LibGit2Sharp;

namespace stalker_gamma.core.ViewModels.Tabs.Queries;

public static class GetStalkerGammaLastCommit
{
    public record Query(string Dir);

    public sealed class Handler
    {
        public string Execute(Query q)
        {
            var repo = new Repository(q.Dir);
            return repo.Head.Tip.Id.ToString();
        }
    }
}
