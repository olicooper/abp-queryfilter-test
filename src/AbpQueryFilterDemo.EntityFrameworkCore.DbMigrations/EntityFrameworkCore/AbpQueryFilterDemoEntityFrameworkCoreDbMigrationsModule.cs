using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AbpQueryFilterDemo.EntityFrameworkCore
{
    [DependsOn(
        typeof(AbpQueryFilterDemoEntityFrameworkCoreModule)
        )]
    public class AbpQueryFilterDemoEntityFrameworkCoreDbMigrationsModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAbpDbContext<AbpQueryFilterDemoMigrationsDbContext>();
        }
    }
}
