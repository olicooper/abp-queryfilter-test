using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace AbpQueryFilterDemo.EntityFrameworkCore
{
#pragma warning disable CA2231 // Overload operator equals on overriding value type Equals
#nullable enable

#if DBPROVIDER_POMELO_MYSQL
    /// <summary>
    /// Wrapper for Pomelo MySql CacheKeyGenerator (Pomelo.EntityFrameworkCore.MySql.Query.Internal.MySqlCompiledQueryCacheKeyGenerator)
    /// see: https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql/blob/b0f744d967c557581ef98df3618300ae1e7cab3e/src/EFCore.MySql/Query/Internal/MySqlCompiledQueryCacheKeyGenerator.cs
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "Required to inject AbpCacheKeyGenerator functionality")]
    public class MySqlCompiledQueryWithAbpFiltersCacheKeyGenerator : Pomelo.EntityFrameworkCore.MySql.Query.Internal.MySqlCompiledQueryCacheKeyGenerator, ICompiledQueryCacheKeyGenerator
    {
        private AbpCacheKeyGenerator _abpCacheKeyGenerator;

        public MySqlCompiledQueryWithAbpFiltersCacheKeyGenerator(
            [NotNull] AbpCacheKeyGenerator abpCacheKeyGenerator,
            [NotNull] CompiledQueryCacheKeyGeneratorDependencies dependencies,
            [NotNull] RelationalCompiledQueryCacheKeyGeneratorDependencies relationalDependencies)
            : base(dependencies, relationalDependencies)
        {
            _abpCacheKeyGenerator = abpCacheKeyGenerator;
        }

        /// <inheritdoc />
        public override object GenerateCacheKey(Expression query, bool async)
           => new CompiledQueryCacheKeyAbpWrapper(
               base.GenerateCacheKey(query, async), 
               _abpCacheKeyGenerator.GenerateCacheKey(query, async));
    }
#else
    /// <summary>
    /// Wrapper for relational CacheKeyGenerator (Pomelo.EntityFrameworkCore.MySql.Query.Internal.MySqlCompiledQueryCacheKeyGenerator)
    /// see: https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql/blob/b0f744d967c557581ef98df3618300ae1e7cab3e/src/EFCore.MySql/Query/Internal/MySqlCompiledQueryCacheKeyGenerator.cs
    /// </summary>
    public class RelationalCompiledQueryWithAbpFiltersCacheKeyGenerator : RelationalCompiledQueryCacheKeyGenerator, ICompiledQueryCacheKeyGenerator
    {
        private AbpCacheKeyGenerator _abpCacheKeyGenerator;

        public RelationalCompiledQueryWithAbpFiltersCacheKeyGenerator(
            [NotNull] AbpCacheKeyGenerator abpCacheKeyGenerator,
            [NotNull] CompiledQueryCacheKeyGeneratorDependencies dependencies,
            [NotNull] RelationalCompiledQueryCacheKeyGeneratorDependencies relationalDependencies)
            : base(dependencies, relationalDependencies)
        {
            _abpCacheKeyGenerator = abpCacheKeyGenerator;
        }

        /// <inheritdoc />
        public override object GenerateCacheKey(Expression query, bool async)
           => new CompiledQueryCacheKeyAbpWrapper(
               base.GenerateCacheKey(query, async), 
               _abpCacheKeyGenerator.GenerateCacheKey(query, async));
    }
#endif

    /// <summary>
    /// Wraps around the source provider (e.g. Pomelo MySql) cache key generator and the ABP cache key generator to combine their functionality without overriding either implementation.
    /// </summary>
    public readonly struct CompiledQueryCacheKeyAbpWrapper : IEquatable<CompiledQueryCacheKeyAbpWrapper>
    {
        private readonly object _sourceCacheKey;
        private readonly object? _abpCacheKey;

        public CompiledQueryCacheKeyAbpWrapper(object sourceCacheKey, object? abpCacheKey)
        {
            _sourceCacheKey = sourceCacheKey;
            _abpCacheKey = abpCacheKey;
        }

        public override bool Equals(object? obj)
            => (obj is CompiledQueryCacheKeyAbpWrapper wrapperKey && Equals(wrapperKey));

        public bool Equals(CompiledQueryCacheKeyAbpWrapper other)
            => _sourceCacheKey.Equals(other._sourceCacheKey)
                && _abpCacheKey != null && _abpCacheKey.Equals(other._abpCacheKey);

        public override int GetHashCode()
            => HashCode.Combine(_sourceCacheKey, _abpCacheKey);
    }

    // see: https://github.com/dotnet/efcore/blob/f54b9dcd189c91fc4b01b79c9387d23095819a8f/src/EFCore.Relational/Query/RelationalCompiledQueryCacheKeyGenerator.cs
    /// <summary>
    /// Base CacheKey implementation for ABP DataFilters. This should be wrapped within an 
    /// instance of <see cref="CompiledQueryCacheKeyAbpWrapper"/> along with the provider specific cache key generator.
    /// </summary>
    public class AbpCacheKeyGenerator
    {
        protected AbpGlobalFiltersOptionsExtension GlobalFiltersExtension;

        public AbpCacheKeyGenerator(
            // todo: create a dependencies class to hold the dependencies below, then inject it via a new Abp database provider
            [NotNull] IDbContextOptions contextOptions)
        {
            GlobalFiltersExtension = contextOptions.FindExtension<AbpGlobalFiltersOptionsExtension>();
        }

        /// <summary>
        /// Generates a cache key object that represents the state of <see cref="IDataFilter"/> for this query.
        /// <br/>This should be combined with the provider-specific cache key!
        /// <br/>No key will be generated if the <see cref="GlobalFiltersExtension"/> is unavailable (returns <see langword="null"/>).
        /// </summary>
        public virtual object? GenerateCacheKey(Expression query, bool async)
        {
            if (AbpQueryFilterDemoConsts.UseCustomFiltering && GlobalFiltersExtension != null)
            {
                var currentFilters = GlobalFiltersExtension.DataFilter.ReadOnlyFilters.Values
                    .Where(filter => filter.IsActive)
                    .Select(filter => new FilterValue(filter.GetType(), filter.IsEnabled))
                    .ToHashSet();

                return new CompiledQueryWithAbpFiltersCacheKey(
                    GlobalFiltersExtension.AbpQueryFiltersDisabled,
                    currentFilters);
            }

            return null;
        }

        protected readonly struct CompiledQueryWithAbpFiltersCacheKey : IEquatable<CompiledQueryWithAbpFiltersCacheKey>
        {
            private readonly bool _ignoreAbpDataFilters;
            private readonly HashSet<FilterValue> _appliedDataFilters;

            public CompiledQueryWithAbpFiltersCacheKey(
                bool ignoreAbpDataFilters,
                HashSet<FilterValue> appliedDataFilters)
            {
                _ignoreAbpDataFilters = ignoreAbpDataFilters;
                _appliedDataFilters = appliedDataFilters;
            }

            public override bool Equals(object? obj)
                => (obj is CompiledQueryWithAbpFiltersCacheKey abpFilterKey && Equals(abpFilterKey));

            public bool Equals(CompiledQueryWithAbpFiltersCacheKey other)
                => _ignoreAbpDataFilters == other._ignoreAbpDataFilters
                    && _appliedDataFilters.SequenceEqual(other._appliedDataFilters);

            public override int GetHashCode()
            {
                var hash = new HashCode();
                hash.Add(_ignoreAbpDataFilters);

                // todo: this must be slow, can we improve it?
                // note: _appliedDataFilters HashSet will be different every time the query is run (so we can't compare _appliedDataFilters as a whole), 
                //       but the objects in the HashSet are always the same.
                //hash.Add(_appliedDataFilters);
                foreach (var filter in _appliedDataFilters)
                {
                    hash.Add(filter);
                }

                return hash.ToHashCode();
            }
        }

        protected struct FilterValue
        {
            public readonly Type Type;
            public readonly bool IsEnabled;

            public FilterValue(Type type, bool isEnabled)
            {
                Type = type;
                IsEnabled = isEnabled;
            }

            public override bool Equals(object? obj)
                => obj is FilterValue value &&
                    IsEnabled == value.IsEnabled &&
                    Type.Equals(value.Type);

            public override int GetHashCode()
                => HashCode.Combine(Type, IsEnabled);
        }
    }
#nullable disable
#pragma warning restore CA2231 // Overload operator equals on overriding value type Equals
}
