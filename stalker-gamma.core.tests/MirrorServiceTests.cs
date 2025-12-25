using System.Threading.Tasks;
using stalker_gamma.core.Services;
using stalker_gamma.core.Services.GammaInstaller.Utilities;
using stalker_gamma.core.Utilities;

namespace stalker_gamma.core.tests;

public class MirrorServiceTests
{
    [Test]
    public async Task TestMirrorService()
    {
        var cs = new CurlService();
        var sut = new MirrorService(cs);

        var result = await sut.GetMirrorAsync("https://www.moddb.com/downloads/start/183466/all");
        ;
    }
}
