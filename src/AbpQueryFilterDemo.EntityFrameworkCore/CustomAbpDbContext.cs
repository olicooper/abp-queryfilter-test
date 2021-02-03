using AbpQueryFilterDemo.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Volo.Abp;
using Volo.Abp.Domain.Entities;
using Volo.Abp.EntityFrameworkCore;

namespace AbpQueryFilterDemo.EntityFrameworkCore
{
    public abstract class CustomAbpDbContext<TDbContext> : AbpDbContext<TDbContext> where TDbContext : DbContext
    {
        protected CustomAbpDbContext(DbContextOptions<TDbContext> options) : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            optionsBuilder.ReplaceService<IQueryTranslationPreprocessorFactory, CustomQueryTranslationPreprocessorFactory>();
        }
    }

    /// <summary>
    ///     <para>
    ///         A class that preprocesses the query before translation.
    ///     </para>
    /// </summary>
    public class CustomQueryTranslationPreprocessor : RelationalQueryTranslationPreprocessor
    {
        private Dictionary<Type, HashSet<LambdaExpression>> _parameterizedQueryFilterPredicateCache
            = new();

        public CustomQueryTranslationPreprocessor(
            [NotNull] QueryTranslationPreprocessorDependencies dependencies,
            [NotNull] RelationalQueryTranslationPreprocessorDependencies relationalDependencies,
            [NotNull] QueryCompilationContext queryCompilationContext)
            : base(dependencies, relationalDependencies, queryCompilationContext) { }

        public override Expression NormalizeQueryableMethod([NotNull] Expression expression)
            => PostNormalizeQueryableMethod(
                base.NormalizeQueryableMethod(
                    PreNormalizeQueryableMethod(Check.NotNull(expression, nameof(expression)))));

        /// <summary>
        /// Run before <see cref="RelationalQueryTranslationPreprocessor.NormalizeQueryableMethod(Expression)"/>.
        /// </summary>
        protected virtual Expression PreNormalizeQueryableMethod([NotNull] Expression expression)
        {
            return new QueryableMethodNormalizingExpressionVisitor(
                QueryCompilationContext,
                ref _parameterizedQueryFilterPredicateCache)
               .Visit(expression);
        }

        /// <summary>
        /// Run after <see cref="RelationalQueryTranslationPreprocessor.NormalizeQueryableMethod(Expression)"/>.
        /// </summary>
        protected virtual Expression PostNormalizeQueryableMethod([NotNull] Expression expression)
        {
            return new QueryableFilteringExpressionVisitor(
                QueryCompilationContext,
                ref _parameterizedQueryFilterPredicateCache)
               .Visit(expression);
        }
    }

    public class CustomQueryTranslationPreprocessorFactory : IQueryTranslationPreprocessorFactory
    {
        protected QueryTranslationPreprocessorDependencies Dependencies { get; }
        protected RelationalQueryTranslationPreprocessorDependencies RelationalDependencies;

        public CustomQueryTranslationPreprocessorFactory(
            QueryTranslationPreprocessorDependencies dependencies,
            RelationalQueryTranslationPreprocessorDependencies relationalDependencies)
        {
            Dependencies = dependencies;
            RelationalDependencies = relationalDependencies;
        }

        public QueryTranslationPreprocessor Create(QueryCompilationContext queryCompilationContext)
            => new CustomQueryTranslationPreprocessor(Dependencies, RelationalDependencies, queryCompilationContext);
    }

    public class QueryableFilteringExpressionVisitor : ExpressionVisitor
    {
        private readonly QueryCompilationContext _queryCompilationContext;
        private Dictionary<Type, HashSet<LambdaExpression>> _parameterizedQueryFilterPredicateCache;

        public QueryableFilteringExpressionVisitor(
            [NotNull] QueryCompilationContext queryCompilationContext,
            [NotNull] ref Dictionary<Type, HashSet<LambdaExpression>> parameterizedQueryFilterPredicateCache
        )
        {
            _queryCompilationContext = queryCompilationContext;
            _parameterizedQueryFilterPredicateCache = parameterizedQueryFilterPredicateCache;
        }

        // The 'ApplyQueryFilter' method: https://github.com/dotnet/efcore/blob/f54b9dcd189c91fc4b01b79c9387d23095819a8f/src/EFCore/Query/Internal/NavigationExpandingExpressionVisitor.cs#L1412-L1462
        /// <summary>
        /// Removes query filters passed in <see cref="AbpQueryableExtensions.IgnoreAbpQueryFilter"/> from a given query expression.
        /// </summary>
        protected override Expression VisitExtension(Expression extensionExpression)
        {
            // Only modify the query once we have reached the root expression (when the expression tree has been traversed/evaluated)
            if (extensionExpression != null && extensionExpression is QueryRootExpression queryRootExpression)
            {
                // Skip query modification if 'IgnoreQueryFilters' has been used OR 'IgnoreAbpGlobalQueryFilters' hasn't been used
                if (!_queryCompilationContext.IgnoreQueryFilters &&
                    _queryCompilationContext.Tags.Contains(AbpQueryableExtensions.IgnoreAbpGlobalQueryFilter))
                {
                    var entityType = queryRootExpression.EntityType;
                    //#pragma warning disable CS0618 // Type or member is obsolete
                    //                    var definingQuery = entityType.GetDefiningQuery();
                    //#pragma warning restore CS0618 // Type or member is obsolete
                    //                    if (definingQuery != null
                    //                        // Apply defining query only when it is not custom query root
                    //                        && queryRootExpression.GetType() == typeof(QueryRootExpression))
                    //                    {
                    //                    }
                    var rootEntityType = entityType.GetRootType();
                    // todo: the queryFilter will probably need to be translated before use: https://github.com/dotnet/efcore/blob/da00fb69d615fa22a83dfee2077ad31b7bd15823/src/EFCore/Query/Internal/QueryableMethodNormalizingExpressionVisitor.cs#L80-L118
                    var queryFilter = rootEntityType.GetQueryFilter();
                }
            }

            return base.VisitExtension(extensionExpression);
        }
    }

    public class QueryableMethodNormalizingExpressionVisitor : ExpressionVisitor
    {
        private readonly QueryCompilationContext _queryCompilationContext;
        private Dictionary<Type, HashSet<LambdaExpression>> _parameterizedQueryFilterPredicateCache;

        public QueryableMethodNormalizingExpressionVisitor(
            [NotNull] QueryCompilationContext queryCompilationContext,
            [NotNull] ref Dictionary<Type, HashSet<LambdaExpression>> parameterizedQueryFilterPredicateCache
        )
        {
            _queryCompilationContext = queryCompilationContext;
            _parameterizedQueryFilterPredicateCache = parameterizedQueryFilterPredicateCache;
        }

        /// <summary>
        /// Extracts, filters and caches all query filters defined in calls to <see cref="AbpQueryableExtensions.IgnoreAbpQueryFilter"/>.
        /// <para>
        ///     This ignores any filters that do not have an associated 
        ///     <see cref="EntityFrameworkQueryableExtensions.Include{TEntity, TProperty}(System.Linq.IQueryable{TEntity}, Expression{Func{TEntity, TProperty}})"/>
        ///     statement
        /// </para>
        /// </summary>
        protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
        {
            Check.NotNull(methodCallExpression, nameof(methodCallExpression));

            // Extract information from query metadata method and prune them
            if (methodCallExpression.Method.DeclaringType == typeof(AbpQueryableExtensions) 
                && methodCallExpression.Method.IsGenericMethod)
            {
                return ExtractIgnoreAbpQueryFilterMetadata(methodCallExpression);
            }
            //else if (method.DeclaringType == typeof())

            return base.VisitMethodCall(methodCallExpression);
        }

#nullable enable
        // May be useful: Microsoft.EntityFrameworkCore.Query.QueryableMethods
        private Expression ExtractIgnoreAbpQueryFilterMetadata(MethodCallExpression methodCallExpression)
        {
            var genericMethodDefinition = methodCallExpression.Method.GetGenericMethodDefinition();

            if (genericMethodDefinition != AbpQueryableExtensions.IgnoreAbpGlobalQueryFilterMethodInfo)
            {
                return methodCallExpression;
            }

            IEntityType? entityType = (methodCallExpression.Arguments[0] as QueryRootExpression)?.EntityType;
            var keySelector = methodCallExpression.Arguments[1];

            // We need to strip/evaluate IgnoreAbpQueryFilter regardless of whether 
            // we do anything with it (otherwise the query can't be evaluated later)
            // todo: we should probably throw an exception here instead of skipping it
            if (keySelector == null/* || entityType == null*/)
            {
                return Visit(methodCallExpression.Arguments[0]);
            }

//#pragma warning disable CS0618 // Type or member is obsolete
//            var definingQuery = entityType.GetDefiningQuery();
//#pragma warning restore CS0618 // Type or member is obsolete


            var lambda = keySelector.UnwrapLambdaFromQuote();
            var type = lambda.Body.Type;

            if (lambda.ReturnType.IsAssignableFrom(typeof(IEntity)))
            {
                // Do stuff for IEntity here...
                var entityToIgnore = type.FullName;

                if (!_parameterizedQueryFilterPredicateCache.ContainsKey(lambda.Parameters[0].Type))
                {
                    _parameterizedQueryFilterPredicateCache.Add(lambda.Parameters[0].Type, new() { lambda });
                }
                else
                {
                    _parameterizedQueryFilterPredicateCache[lambda.Parameters[0].Type].Add(lambda);
                }
            }
            else if (lambda.ReturnType.IsAssignableFrom(typeof(IEnumerable<IEntity>)))
            {
                foreach (var i in type.GetInterfaces())
                    if (i.IsGenericType && i.GetGenericTypeDefinition() is IEntity)
                    {
                        // Do stuff for ICollection<IEntity> here...
                        var entityToIgnore = i.GetGenericArguments()[0].FullName;

                        if (!_parameterizedQueryFilterPredicateCache.ContainsKey(lambda.Parameters[0].Type))
                        {
                            _parameterizedQueryFilterPredicateCache.Add(lambda.Parameters[0].Type, new() { lambda });
                        }
                        else
                        {
                            _parameterizedQueryFilterPredicateCache[lambda.Parameters[0].Type].Add(lambda);
                        }
                    }
            }


            //var keySelectors = methodCallExpression.Arguments[1] as NewArrayExpression;
            //if (keySelectors == null || keySelectors.Expressions.Count == 0) return null;

            //foreach (var selector in keySelectors.Expressions)
            //{
            //    var lambda = selector.UnwrapLambdaFromQuote();
            //    var type = lambda.Body.Type;

            //    if (lambda.ReturnType.IsAssignableFrom(typeof(IEntity)))
            //    {
            //        var entityToIgnore = type.FullName;
            //    }
            //    else if (lambda.ReturnType.IsAssignableFrom(typeof(IEnumerable<IEntity>)))
            //    {
            //        foreach (var i in type.GetInterfaces())
            //            if (i.IsGenericType && i.GetGenericTypeDefinition() is IEntity)
            //                if (i.IsGenericType && i.GetGenericTypeDefinition() is IEntity)
            //                {
            //                    var entityToIgnore = i.GetGenericArguments()[0].FullName;
            //                }
            //    }
            //}

            _queryCompilationContext.AddTag(AbpQueryableExtensions.IgnoreAbpGlobalQueryFilter);

            return Visit(methodCallExpression.Arguments[0]);
        }
#nullable disable
    }

    public static class ExpressionExtensions
    {
        // https://github.com/dotnet/efcore/blob/da00fb69d615fa22a83dfee2077ad31b7bd15823/src/Shared/ExpressionExtensions.cs#L20
        public static LambdaExpression UnwrapLambdaFromQuote(this Expression expression)
            => (LambdaExpression)(expression is UnaryExpression unary && expression.NodeType == ExpressionType.Quote
                ? unary.Operand
                : expression);
    }
}
