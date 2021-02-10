using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace AbpQueryFilterDemo.EntityFrameworkCore
{
    // see: https://github.com/dotnet/efcore/blob/f54b9dcd189c91fc4b01b79c9387d23095819a8f/src/EFCore.Relational/Query/RelationalCompiledQueryCacheKeyGenerator.cs
    public class CompiledQueryWithAbpFiltersCacheKeyGenerator : RelationalCompiledQueryCacheKeyGenerator, ICompiledQueryCacheKeyGenerator
    {
        protected AbpGlobalFiltersOptionsExtension GlobalFiltersExtension;

        public CompiledQueryWithAbpFiltersCacheKeyGenerator(
            [NotNull] CompiledQueryCacheKeyGeneratorDependencies dependencies,
            [NotNull] RelationalCompiledQueryCacheKeyGeneratorDependencies relationalDependencies,
            // todo: create a dependencies class to hold the dependencies below, then inject it via a new Abp database provider
            [NotNull] IDbContextOptions contextOptions)
            : base(dependencies, relationalDependencies)
        {
            GlobalFiltersExtension = contextOptions.FindExtension<AbpGlobalFiltersOptionsExtension>();
        }

        /// <inheritdoc />
        public override object GenerateCacheKey(Expression query, bool async)
           => GenerateCacheKeyCore(query, async);

        protected new object GenerateCacheKeyCore(Expression query, bool async)
        {
            if (AbpQueryFilterDemoConsts.UseCustomFiltering && GlobalFiltersExtension != null)
            {
                // todo: A change in the quantity of values will invalidate the cache key. 
                //       We need to determine which were applied to this specific query.
                var currentFilters = GlobalFiltersExtension.FilterCollection.Values
                    .Select(x => GetFilterValue(x))
                    .ToHashSet();

                return new CompiledQueryWithAbpFiltersCacheKey(
                    base.GenerateCacheKeyCore(query, async),
                    GlobalFiltersExtension.AbpQueryFiltersDisabled,
                    currentFilters);
            }
            else
            {
                return base.GenerateCacheKeyCore(query, async);
            }
        }

        private static FilterValue GetFilterValue(object obj)
        {
            var type = obj.GetType();
            bool isEnabled = false;

            try
            {
                // todo: remove this once IDataFilter is updated :)
                isEnabled = (bool)type
                .GetProperty("IsEnabled", BindingFlags.Public | BindingFlags.Instance)
                .GetValue(obj);
            }
            catch { }

            return new FilterValue(type, isEnabled);
        }

        protected readonly struct CompiledQueryWithAbpFiltersCacheKey : IEquatable<CompiledQueryWithAbpFiltersCacheKey>
        {
            private readonly object _compiledQueryCacheKey;
            private readonly bool _ignoreAbpDataFilters;
            private readonly HashSet<FilterValue> _appliedDataFilters;

            public CompiledQueryWithAbpFiltersCacheKey(
                object compiledQueryCacheKey,
                bool ignoreAbpDataFilters,
                HashSet<FilterValue> appliedDataFilters)
            {
                _compiledQueryCacheKey = compiledQueryCacheKey;
                _ignoreAbpDataFilters = ignoreAbpDataFilters;
                _appliedDataFilters = appliedDataFilters;
            }

#nullable enable
            public override bool Equals(object? obj)
                => (obj is CompiledQueryWithAbpFiltersCacheKey abpFilterKey && Equals(abpFilterKey));
#nullable disable

            public bool Equals(CompiledQueryWithAbpFiltersCacheKey other)
                => _compiledQueryCacheKey.Equals(other._compiledQueryCacheKey)
                    && _ignoreAbpDataFilters == other._ignoreAbpDataFilters
                    && _appliedDataFilters.SequenceEqual(other._appliedDataFilters);

            public override int GetHashCode()
            {
                var hash = new HashCode();
                hash.Add(_compiledQueryCacheKey);
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

            public override bool Equals(object obj)
                => obj is FilterValue value &&
                    IsEnabled == value.IsEnabled &&
                    Type.Equals(value.Type);

            public override int GetHashCode()
                => HashCode.Combine(Type, IsEnabled);
        }
    }
}
