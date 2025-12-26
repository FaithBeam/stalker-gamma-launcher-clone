using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using stalker_gamma.core.Factories;
using stalker_gamma.core.Models;

namespace stalker_gamma_gui.ViewModels.Tabs.MainTab.Queries;

public static class GetModDownloadExtractVms
{
    public sealed class Handler(ModListRecordFactory modListRecordFactory, IHttpClientFactory hcf)
    {
        public async Task<IList<ModListRecord>> ExecuteAsync()
        {
            string stalkerGammaModsTxt;
            try
            {
                var getStalkerGammaModsResponse = await _hc.GetAsync(StalkerGammaApi);
                getStalkerGammaModsResponse.EnsureSuccessStatusCode();
                stalkerGammaModsTxt = await getStalkerGammaModsResponse.Content.ReadAsStringAsync();
            }
            catch (Exception e)
            {
                throw new GetModDownloadExtractVmsException(
                    $"Error downloading {StalkerGammaApi}",
                    e
                );
            }

            return stalkerGammaModsTxt
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(_modListRecordFactory.Create)
                .Cast<ModListRecord>()
                // .Where(x => x is not GithubRecord)
                // .Select(_modDownloadExtractProgressVmFactory.Create)
                .ToList();
        }

        // private readonly ModDownloadExtractProgressVmFactory _modDownloadExtractProgressVmFactory =
        //     modDownloadExtractProgressVmFactory;
        private readonly ModListRecordFactory _modListRecordFactory = modListRecordFactory;
        private readonly HttpClient _hc = hcf.CreateClient();

        private const string StalkerGammaApi = "https://stalker-gamma.com/api/list?key=";
    }
}

public class GetModDownloadExtractVmsException(string msg, Exception ex) : Exception(msg, ex);
