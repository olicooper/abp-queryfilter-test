﻿using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Threading;
using Volo.Abp.MultiTenancy;

namespace AbpQueryFilterDemo.EntityFrameworkCore
{
    // Original from: https://github.com/dotnet/efcore/blob/b8483772f298f5ada8b2b5253a9904c93c34919f/test/EFCore.Tests/ServiceProviderCacheTest.cs#L226-L264
    public class AbpGlobalFiltersOptionsExtension : IDbContextOptionsExtension
    {
        public DbContextOptionsExtensionInfo Info => _info ??= new ExtensionInfo(this);
        private DbContextOptionsExtensionInfo _info;

        public AbpQueryFilterDemo.IDataFilter DataFilter { get; }

        // todo: is this the best way to get the tenant? What if multitenancy is not available?
        public ICurrentTenantAccessor CurrentTenantAccessor { get; } = null;

        public bool AbpQueryFiltersDisabled => _abpQueryFiltersDisabled.Value;
        protected readonly AsyncLocal<bool> _abpQueryFiltersDisabled;

        // copy constructor
        public AbpGlobalFiltersOptionsExtension(AbpGlobalFiltersOptionsExtension other, ICurrentTenantAccessor currentTenantAccessor)
        {
            _info = other._info;
            DataFilter = other.DataFilter;
            CurrentTenantAccessor = currentTenantAccessor ?? other.CurrentTenantAccessor;
            _abpQueryFiltersDisabled = other._abpQueryFiltersDisabled;
        }

        // Allows injecting services from outside EntityFramework's self-managed DI container
        // see: https://blog.oneunicorn.com/2016/10/27/dependency-injection-in-ef-core-1-1/
        public AbpGlobalFiltersOptionsExtension(
            AbpQueryFilterDemo.IDataFilter dataFilter,
            ICurrentTenantAccessor currentTenantAccessor)
        {
            DataFilter = dataFilter;
            CurrentTenantAccessor = currentTenantAccessor;
            _abpQueryFiltersDisabled = new AsyncLocal<bool>();
        }

        public void SetAbpQueryFiltersDisabled(bool value = true)
        {
            _abpQueryFiltersDisabled.Value = value;
        }

        // configures the required services for the extension with the service collection scoped to the current request.
        public virtual void ApplyServices(IServiceCollection servicesCollection)
        {
            // Register the AbpCacheKeyGenerator so it can be injected to the RelationalCompiledQueryCacheKeyGenerator instance
            new EntityFrameworkRelationalServicesBuilder(servicesCollection)
                .TryAddProviderSpecificServices(m => m
                    .GetInfrastructure()
                        .AddDependencyScoped<AbpCacheKeyGenerator>())
                .TryAddCoreServices();
        }

        public virtual void Validate(IDbContextOptions options) { }

        private sealed class ExtensionInfo : DbContextOptionsExtensionInfo
        {
            public ExtensionInfo(IDbContextOptionsExtension extension) : base(extension) { }
            public override bool IsDatabaseProvider => false;
            public override long GetServiceProviderHashCode() => 0;
            // todo: list more debug info (i.e. tenant info and data filters) in log output
            public override string LogFragment => "Using AbpGlobalFiltersExtension";
            public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
            {
                debugInfo["AbpGlobalFilters"] = "1";
            }
        }
    }
}
