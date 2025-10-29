namespace stalker_gamma.core.ViewModels.Tabs.MainTab.Commands;

public interface IUserLtxReplaceFullscreenWithBorderlessFullscreen
{
    public Task<bool?> ExecuteAsync(UserLtxReplaceFullscreenWithBorderlessFullscreen.Command c);
}

public static class UserLtxReplaceFullscreenWithBorderlessFullscreen
{
    public sealed record Command(string PathToUserLtx);

    public sealed class Handler : IUserLtxReplaceFullscreenWithBorderlessFullscreen
    {
        /// <summary>
        /// null = file not found
        /// false = file does not contain rs_screenmode fullscreen
        /// true = file edited to rs_screenmode borderless
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public async Task<bool?> ExecuteAsync(Command c)
        {
            if (File.Exists(c.PathToUserLtx))
            {
                var text = await File.ReadAllTextAsync(c.PathToUserLtx);
                if (text.Contains("rs_screenmode fullscreen"))
                {
                    await File.WriteAllTextAsync(
                        c.PathToUserLtx,
                        text.Replace("rs_screenmode fullscreen", "rs_screenmode borderless")
                    );
                    return true;
                }

                return false;
            }
            return null;
        }
    }
}
