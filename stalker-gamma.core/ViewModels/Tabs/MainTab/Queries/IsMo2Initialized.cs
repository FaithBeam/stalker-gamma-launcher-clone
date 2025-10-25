namespace stalker_gamma.core.ViewModels.Tabs.MainTab.Queries;

public interface IIsMo2Initialized
{
    public bool Execute(IsMo2Initialized.Query q);
}

public static class IsMo2Initialized
{
    public sealed record Query(string PathToMo2Ini);

    public sealed class Handler : IIsMo2Initialized
    {
        public bool Execute(Query q) => File.Exists(q.PathToMo2Ini);
    }
}
