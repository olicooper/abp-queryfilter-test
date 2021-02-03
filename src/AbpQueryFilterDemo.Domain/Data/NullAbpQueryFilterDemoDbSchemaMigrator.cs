using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace AbpQueryFilterDemo.Data
{
    /* This is used if database provider does't define
     * IAbpQueryFilterDemoDbSchemaMigrator implementation.
     */
    public class NullAbpQueryFilterDemoDbSchemaMigrator : IAbpQueryFilterDemoDbSchemaMigrator, ITransientDependency
    {
        public Task MigrateAsync()
        {
            return Task.CompletedTask;
        }
    }
}