using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AbpQueryFilterDemo.Data;
using Volo.Abp.DependencyInjection;

namespace AbpQueryFilterDemo.EntityFrameworkCore
{
    public class EntityFrameworkCoreAbpQueryFilterDemoDbSchemaMigrator
        : IAbpQueryFilterDemoDbSchemaMigrator, ITransientDependency
    {
        private readonly IServiceProvider _serviceProvider;

        public EntityFrameworkCoreAbpQueryFilterDemoDbSchemaMigrator(
            IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task MigrateAsync()
        {
            /* We intentionally resolving the AbpQueryFilterDemoMigrationsDbContext
             * from IServiceProvider (instead of directly injecting it)
             * to properly get the connection string of the current tenant in the
             * current scope.
             */

            await _serviceProvider
                .GetRequiredService<AbpQueryFilterDemoMigrationsDbContext>()
                .Database
                .MigrateAsync();
        }
    }
}