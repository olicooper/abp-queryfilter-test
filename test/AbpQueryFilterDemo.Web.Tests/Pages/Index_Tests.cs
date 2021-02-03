using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace AbpQueryFilterDemo.Pages
{
    public class Index_Tests : AbpQueryFilterDemoWebTestBase
    {
        [Fact]
        public async Task Welcome_Page()
        {
            var response = await GetResponseAsStringAsync("/");
            response.ShouldNotBeNull();
        }
    }
}
