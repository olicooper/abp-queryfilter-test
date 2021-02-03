using AbpQueryFilterDemo.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.Modularity;

namespace AbpQueryFilterDemo.DbMigrator
{
    [DependsOn(
        typeof(AbpAutofacModule),
        typeof(AbpQueryFilterDemoEntityFrameworkCoreDbMigrationsModule),
        typeof(AbpQueryFilterDemoApplicationContractsModule)
        )]
    public class AbpQueryFilterDemoDbMigratorModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<AbpBackgroundJobOptions>(options => options.IsJobExecutionEnabled = false);
        }
    }
}
