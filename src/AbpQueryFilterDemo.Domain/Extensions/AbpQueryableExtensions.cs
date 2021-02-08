//using Microsoft.EntityFrameworkCore.Query.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Volo.Abp;

namespace System.Linq
{
    public static class AbpQueryableExtensions_DemoProj
    {
        //public static readonly string IgnoreTargetedAbpQueryFilterTag = "IgnoreTargetedAbpQueryFilter";

        //public static readonly MethodInfo IgnoreTargetedAbpQueryFilterMethodInfo
        //    = typeof(AbpQueryableExtensions_DemoProj)
        //        .GetTypeInfo().GetMethod(nameof(IgnoreAbpQueryFilterInternal), BindingFlags.NonPublic | BindingFlags.Static);

        //// todo: It would be better to use Microsoft.EntityFrameworkCore.Query.IIncludableQueryable - but we cant depend on EntityFrameworkCore to use it in AppServices
        ///// <inheritdoc cref="IgnoreAbpQueryFilter{TEntity, TSelector}(IQueryable{TEntity}, Expression{Func{TEntity, TSelector}})"/>
        //public static IQueryable<TEntity> IgnoreAbpQueryFilter<TEntity>(
        //    [NotNull] this IQueryable<TEntity> source,
        //    [NotNull] Expression<Func<TEntity, Volo.Abp.Domain.Entities.IEntity>> keySelector)
        //    where TEntity : class, Volo.Abp.Domain.Entities.IEntity
        //    => IgnoreAbpQueryFilterInternal(source, keySelector);

        ///// <inheritdoc cref="IgnoreAbpQueryFilter{TEntity, TSelector}(IQueryable{TEntity}, Expression{Func{TEntity, TSelector}})"/>
        //public static IQueryable<TEntity> IgnoreAbpQueryFilter<TEntity>(
        //    [NotNull] this IQueryable<TEntity> source,
        //    [NotNull] Expression<Func<TEntity, IEnumerable<Volo.Abp.Domain.Entities.IEntity>>> keySelector)
        //    where TEntity : class, Volo.Abp.Domain.Entities.IEntity
        //    => IgnoreAbpQueryFilterInternal(source, keySelector);

        ///// <summary>
        /////     Specifies that the current Entity Framework LINQ query should not have any model-level entity query filters applied for the specified child entity.
        ///// </summary>
        ///// <typeparam name="TEntity"> The type of entity being queried. </typeparam>
        ///// <typeparam name="TSelector"> The type of entity being ignored. </typeparam>
        ///// <param name="source"> The source query. </param>
        ///// <param name="keySelector"> The entity or collection to not apply global query filters to. </param>
        ///// <returns> A new query that will not apply any model-level entity query filters for the child entity. </returns>
        ///// <exception cref="ArgumentNullException"> <paramref name="source" /> is <see langword="null" />. </exception>
        //private static IQueryable<TEntity> IgnoreAbpQueryFilterInternal<TEntity, TSelector>(
        //    [NotNull] this IQueryable<TEntity> source,
        //    [NotNull] Expression<Func<TEntity, TSelector>> keySelector)
        //    where TEntity : class, Volo.Abp.Domain.Entities.IEntity
        //{
        //    Check.NotNull(source, nameof(source));
        //    Check.NotNull(keySelector, nameof(keySelector));

        //    return
        //        //source.Provider is EntityQueryProvider ? 
        //        source.Provider.CreateQuery<TEntity>(
        //                Expression.Call(
        //                    instance: null,
        //                    method: IgnoreTargetedAbpQueryFilterMethodInfo.MakeGenericMethod(
        //                        typeof(TEntity),
        //                        typeof(TSelector)
        //                    ),
        //                    source.Expression,
        //                    keySelector
        //                //Expression.NewArrayInit(typeof(Expression<Func<TEntity, TSelector>>), keySelectors)
        //                )
        //            )
        //        //: source
        //        ;
        //}

        // todo: I don't like exposing this to public, can this be placed in another namespace? I have created a copy in CustomAbpDbContext for now
        internal static readonly MethodInfo IgnoreAbpQueryFiltersMethodInfo
            = typeof(AbpQueryableExtensions_DemoProj)
                .GetTypeInfo()
                .GetDeclaredMethod(nameof(IgnoreAbpQueryFilters));

        /// <summary>
        ///     Specifies that the current Linq query should not have any ABP model-level entity query filters applied.
        ///     <br/> Examples include <see cref="ISoftDelete"/> and <see cref="IMultiTenant"/>.
        /// </summary>
        /// <typeparam name="TEntity"> The type of entity being queried. </typeparam>
        /// <param name="source"> The source query. </param>
        /// <returns> A new query that will not apply any model-level entity query filters. </returns>
        /// <exception cref="ArgumentNullException"> <paramref name="source" /> is <see langword="null" />. </exception>
        public static IQueryable<TEntity> IgnoreAbpQueryFilters<TEntity>(
            [NotNull] this IQueryable<TEntity> source)
            where TEntity : class, Volo.Abp.Domain.Entities.IEntity
        {
            Check.NotNull(source, nameof(source));

            return
                //source.Provider is EntityQueryProvider ?
                source.Provider.CreateQuery<TEntity>(
                    Expression.Call(
                        instance: null,
                        method: IgnoreAbpQueryFiltersMethodInfo.MakeGenericMethod(typeof(TEntity)),
                        source.Expression
                    )
                )
                //: source
                ;
        }
    }
}
