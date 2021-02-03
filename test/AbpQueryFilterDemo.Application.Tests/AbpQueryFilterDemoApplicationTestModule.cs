using Volo.Abp.Modularity;

namespace AbpQueryFilterDemo
{
    [DependsOn(
        typeof(AbpQueryFilterDemoApplicationModule),
        typeof(AbpQueryFilterDemoDomainTestModule)
        )]
    public class AbpQueryFilterDemoApplicationTestModule : AbpModule
    {

    }
}