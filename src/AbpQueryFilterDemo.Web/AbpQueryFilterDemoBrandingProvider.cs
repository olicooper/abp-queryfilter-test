using Volo.Abp.Ui.Branding;
using Volo.Abp.DependencyInjection;

namespace AbpQueryFilterDemo.Web
{
    [Dependency(ReplaceServices = true)]
    public class AbpQueryFilterDemoBrandingProvider : DefaultBrandingProvider
    {
        public override string AppName => "AbpQueryFilterDemo";
    }
}
