using AbpQueryFilterDemo.EntityFrameworkCore;
using Volo.Abp.Modularity;

namespace AbpQueryFilterDemo
{
    [DependsOn(
        typeof(AbpQueryFilterDemoEntityFrameworkCoreTestModule)
        )]
    public class AbpQueryFilterDemoDomainTestModule : AbpModule
    {

    }
}