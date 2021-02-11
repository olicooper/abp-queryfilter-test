using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Options;
using System;
using System.Linq.Expressions;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.MultiTenancy;

// Namespace should be 'Volo.Abp.EntityFrameworkCore.MySQL' according to: https://docs.microsoft.com/en-us/ef/core/providers/writing-a-provider#suggested-naming-of-third-party-providers
namespace AbpQueryFilterDemo.EntityFrameworkCore
{
    public abstract class CustomAbpDbContext<TDbContext> : AbpDbContext<TDbContext> where TDbContext : DbContext
    {
        protected CustomAbpDbContext(DbContextOptions<TDbContext> options) : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            optionsBuilder.ReplaceService<IQueryTranslationPreprocessorFactory, CustomQueryTranslationPreprocessorFactory>();

            // todo: LazyServiceProvider is null when using powershell 'add-migration' etc. this needs investigating!
            if (AbpQueryFilterDemoConsts.UseCustomFiltering && LazyServiceProvider != null)
            {
                optionsBuilder.ReplaceService<ICompiledQueryCacheKeyGenerator, CompiledQueryWithAbpFiltersCacheKeyGenerator>();

                // Custom Extension to access DataFilter and CurrentTenant 
                // todo: does this work okay when the when the IServiceProvider is changed within the query context?
                var extension = optionsBuilder.Options.FindExtension<AbpGlobalFiltersOptionsExtension>()
                    ?? new AbpGlobalFiltersOptionsExtension(
                        LazyServiceProvider.LazyGetRequiredService<AbpQueryFilterDemo.IDataFilter>(), 
                        LazyServiceProvider.LazyGetRequiredService<ICurrentTenantAccessor>());

                ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);
            }
        }

#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member

        [Obsolete]
        protected override void ConfigureGlobalFilters<TEntity>(ModelBuilder modelBuilder, IMutableEntityType mutableEntityType)
            where TEntity : class
        {
            if (AbpQueryFilterDemoConsts.UseCustomFiltering) return;

            base.ConfigureGlobalFilters<TEntity>(modelBuilder, mutableEntityType);
        }

        [Obsolete]
        protected override Expression<Func<TEntity, bool>> CreateFilterExpression<TEntity>()
           where TEntity : class
        {
            if (AbpQueryFilterDemoConsts.UseCustomFiltering) return null; // DISABLE
            
            return base.CreateFilterExpression<TEntity>();
        }

#pragma warning restore CS0809 // Obsolete member overrides non-obsolete member
    }
}
