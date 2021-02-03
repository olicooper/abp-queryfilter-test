using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Volo.Abp;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace AbpQueryFilterDemo.EntityFrameworkCore
{
    public static class AbpEntityFrameworkQueryableExtensions
    {
        public static readonly string IgnoreAbpGlobalQueryFilter = "IgnoreAbpGlobalQueryFilter";

        internal static readonly MethodInfo IgnoreAbpGlobalQueryFilterMethodInfo
            = typeof(AbpEntityFrameworkQueryableExtensions)
                .GetTypeInfo().GetDeclaredMethod(
                nameof(IgnoreAbpQueryFilter));

        //internal static readonly MethodInfo IgnoreQueryFiltersMethodInfo
        //    = typeof(Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions)
        //        .GetTypeInfo().GetDeclaredMethod(
        //        nameof(Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.IgnoreQueryFilters));

        public static IQueryable<TEntity> IgnoreAbpQueryFilter<TEntity>(
            [NotNull] this IQueryable<TEntity> source,
            [NotNull] Expression<Func<TEntity, IEntity>> keySelector)
            where TEntity : class, Volo.Abp.Domain.Entities.IEntity
            => IgnoreAbpQueryFilter(source, keySelector);
        public static IQueryable<TEntity> IgnoreAbpQueryFilter<TEntity>(
            [NotNull] this IQueryable<TEntity> source,
            [NotNull] Expression<Func<TEntity, IEnumerable<IEntity>>> keySelector)
            where TEntity : class, Volo.Abp.Domain.Entities.IEntity
            => IgnoreAbpQueryFilter(source, keySelector);

        /// <summary>
        ///     Specifies that the current Entity Framework LINQ query should not have any model-level entity query filters applied.
        /// </summary>
        /// <typeparam name="TEntity"> The type of entity being queried. </typeparam>
        /// <param name="source"> The source query. </param>
        /// <returns> A new query that will not apply any model-level entity query filters. </returns>
        /// <exception cref="ArgumentNullException"> <paramref name="source" /> is <see langword="null" />. </exception>
        private static IQueryable<TEntity> IgnoreAbpQueryFilter<TEntity, TSelector>(
            [NotNull] this IQueryable<TEntity> source,
            [NotNull] Expression<Func<TEntity, TSelector>> keySelector)
            where TEntity : class, Volo.Abp.Domain.Entities.IEntity
        {
            Check.NotNull(source, nameof(source));
            Check.NotNull(keySelector, nameof(keySelector));

            //return 
            //    Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            //    .IgnoreQueryFilters<TEntity>(source);

            //var arguments = new List<Expression>(keySelectors.Length + 1) { source.Expression };
            //arguments.AddRange(keySelectors);
            //var keySelectorParameter = Expression.Parameter(typeof(Expression<Func<TEntity, TSelector>>[]), "keySelectorParameter");

            // Expression<Func<EmailRule, Expression<Func<EmailRule,IEntity>>[]>>[]

            return
                source.Provider is EntityQueryProvider
                    ? source.Provider.CreateQuery<TEntity>(
                        Expression.Call(
                            instance: null,
                            method: IgnoreAbpGlobalQueryFilterMethodInfo.MakeGenericMethod(
                                typeof(TEntity),
                                typeof(TSelector)
                            ),
                            source.Expression,
                            keySelector
                        //Expression.NewArrayInit(typeof(Expression<Func<TEntity, TSelector>>), keySelectors)
                        )
                    ) : source;
        }
    }
}
