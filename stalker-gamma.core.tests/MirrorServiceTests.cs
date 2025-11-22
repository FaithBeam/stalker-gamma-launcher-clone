using System.Net.Http;
using System.Threading.Tasks;
using NSubstitute;
using stalker_gamma.core.Services;
using stalker_gamma.core.Services.GammaInstaller.Utilities;

namespace stalker_gamma.core.tests;

public class MirrorServiceTests
{
    [Test]
    public async Task TestMirrorService()
    {
        var hcf = Substitute.For<IHttpClientFactory>();
        hcf.CreateClient(Arg.Any<string>()).Returns(new HttpClient());
        var cs = new CurlService(hcf, new OperatingSystemService());
        var sut = new MirrorService(cs);

        var result = await sut.GetMirrorAsync("https://www.moddb.com/downloads/start/183466/all");
        ;
    }
}
