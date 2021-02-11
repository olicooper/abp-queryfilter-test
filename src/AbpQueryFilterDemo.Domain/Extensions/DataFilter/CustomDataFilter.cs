using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Volo.Abp;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;

namespace AbpQueryFilterDemo
{
    public class DataFilterState
    {
        public bool IsActive { get; set; }
        public bool IsEnabled { get; set; }

        public DataFilterState(bool isEnabled, bool isActive = false)
        {
            IsEnabled = isEnabled;
            IsActive = isActive;
        }

        public DataFilterState Clone()
        {
            return new DataFilterState(IsEnabled, IsActive);
        }
    }

    public interface IDataFilter<TFilter> : IBasicDataFilter where TFilter : class { }
    public interface IBasicDataFilter
    {
        IDisposable Enable();
        IDisposable Disable();
        bool IsActive { get; }
        bool IsEnabled { get; }
    }

    public interface IDataFilter : ISingletonDependency
    {
        /// <summary>
        /// The filters that are currently active.
        /// </summary>
        IReadOnlyDictionary<Type, IBasicDataFilter> ReadOnlyFilters { get; }
        /// <summary>
        /// The current <see cref="AbpDataFilterOptions.DefaultStates"/>.
        /// </summary>
        IReadOnlyDictionary<Type, DataFilterState> DefaultFilterStates { get; }

        IDisposable Enable<TFilter>() where TFilter : class;

        IDisposable Disable<TFilter>() where TFilter : class;

        bool IsActive<TFilter>() where TFilter : class;

        bool IsActive(Type filterType);

        bool IsEnabled<TFilter>(bool cacheResult = true) where TFilter : class;

        bool IsEnabled(Type filterType, bool cacheResult = true);

        IDataFilter<TFilter> GetFilter<TFilter>(bool cacheResult = true) where TFilter : class;

        IBasicDataFilter GetFilter(Type filterType, bool cacheResult = true);
    }

    public class DataFilter : IDataFilter
    {
        public IReadOnlyDictionary<Type, IBasicDataFilter> ReadOnlyFilters => Filters;

        public IReadOnlyDictionary<Type, DataFilterState> DefaultFilterStates { get; } = new Dictionary<Type, DataFilterState>();

        protected readonly ConcurrentDictionary<Type, IBasicDataFilter> Filters = new();

        protected readonly AbpDataFilterOptions FilterOptions;

        protected readonly IServiceProvider ServiceProvider;

        public DataFilter(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        public virtual IDisposable Enable<TFilter>() where TFilter : class
        {
            return GetFilter<TFilter>().Enable();
        }

        public virtual IDisposable Disable<TFilter>() where TFilter : class
        {
            return GetFilter<TFilter>().Disable();
        }

        public virtual bool IsActive<TFilter>() where TFilter : class
        {
            return GetFilter<TFilter>(false)?.IsActive ?? false;
        }

        public virtual bool IsActive(Type filterType)
        {
            return GetFilter(filterType, false)?.IsActive ?? false;
        }

        public virtual bool IsEnabled<TFilter>(bool cacheResult = true)
            where TFilter : class
        {
            return GetFilter<TFilter>(cacheResult).IsEnabled;
        }

        public virtual bool IsEnabled(Type filterType, bool cacheResult = true)
        {
            var foundFilter = GetFilter(filterType, cacheResult);
            if (foundFilter != null)
            {
                return foundFilter.IsEnabled;
            }

            return DefaultFilterStates.GetOrDefault(filterType)?.IsEnabled ?? true;
        }

        public virtual IDataFilter<TFilter> GetFilter<TFilter>(bool cacheResult = true)
            where TFilter : class
        {
            if (cacheResult)
            {
                return Filters.GetOrAdd(
                    typeof(TFilter),
                    () => ServiceProvider.GetRequiredService<IDataFilter<TFilter>>()
                ) as IDataFilter<TFilter>;
            }
            else
            {
                // note: not using GetFilter(Type...) because this will be more performant

                if (Filters.TryGetValue(typeof(TFilter), out var value))
                {
                    return (IDataFilter<TFilter>)value;
                }

                return ServiceProvider.GetRequiredService<IDataFilter<TFilter>>();
            }
        }

        public virtual IBasicDataFilter GetFilter(Type filterType, bool cacheResult = true)
        {
            if (filterType == null
                // Should have no more than 1 interface type argument
                || filterType.GenericTypeArguments.Length > 1
                // Should be a generic filter interface e.g. ISoftDelete
                || (filterType.GenericTypeArguments.Length == 0 && !filterType.IsInterface)
                // Should be a filter interface with a concrete parameter e.g. Blog (filter == ISoftDelete<Blog>)
                || (filterType.GenericTypeArguments.Length == 1 && filterType.GenericTypeArguments[0].IsGenericType))
            {
                throw new AbpException($"The {nameof(filterType)} '{(filterType == null ? "<null>" : filterType.Name)}' is not a valid data filter type");
            }

            if (cacheResult)
            {
                return Filters.GetOrAdd(
                    filterType,
                    (type) => ServiceProvider.GetRequiredService(
                        typeof(IDataFilter<>).MakeGenericType(type)) as IBasicDataFilter
                );
            }
            else
            {
                if (Filters.TryGetValue(filterType, out var value))
                {
                    return value;
                }

                return ServiceProvider.GetRequiredService(
                    typeof(IDataFilter<>).MakeGenericType(filterType)) as IBasicDataFilter;
            }
        }
    }

    public class DataFilter<TFilter> : IDataFilter<TFilter> where TFilter : class
    {
        public virtual bool IsActive => Filter.Value != null && Filter.Value.IsActive;

        public virtual bool IsEnabled
        {
            get
            {
                EnsureInitialized();
                return Filter.Value.IsEnabled;
            }
        }

        protected readonly AbpDataFilterOptions Options;

        protected readonly AsyncLocal<DataFilterState> Filter;

        public DataFilter(IOptions<AbpDataFilterOptions> options)
        {
            Options = options.Value;
            Filter = new AsyncLocal<DataFilterState>();
        }

        public virtual IDisposable Enable()
        {
            EnsureInitialized();

            Filter.Value.IsActive = true;

            if (IsEnabled)
            {
                return new DisposeAction(() => {
                    Filter.Value.IsActive = false;
                });
            }

            Filter.Value.IsEnabled = true;

            return new DisposeAction(() => {
                Filter.Value.IsActive = false;
                Filter.Value.IsEnabled = false;
            });
        }

        public virtual IDisposable Disable()
        {
            EnsureInitialized();

            Filter.Value.IsActive = true;

            if (!IsEnabled)
            {
                return new DisposeAction(() => {
                    Filter.Value.IsActive = false;
                });
            }

            Filter.Value.IsEnabled = false;

            return new DisposeAction(() => {
                Filter.Value.IsActive = false;
                Filter.Value.IsEnabled = true;
            });
        }

        protected virtual void EnsureInitialized()
        {
            if (Filter.Value != null)
            {
                return;
            }

            Filter.Value = new DataFilterState(true, false);
        }
    }
}
