using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Volo.Abp;
using Volo.Abp.Data;
using Volo.Abp.MultiTenancy;

namespace AbpQueryFilterDemo.EntityFrameworkCore
{
    public class AbpFilterAppendingExpressionVisitor : ExpressionVisitor
    {
        internal static readonly string AbpQueryFiltersAppliedTag = "AbpQueryFiltersApplied";

        internal static readonly string IgnoreAbpQueryFiltersTag = nameof(AbpQueryableExtensions_DemoProj.IgnoreAbpQueryFilters);

        internal static readonly MethodInfo IgnoreAbpQueryFiltersMethodInfo
            = typeof(AbpQueryableExtensions_DemoProj)
                .GetTypeInfo()
                .GetDeclaredMethod(nameof(AbpQueryableExtensions_DemoProj.IgnoreAbpQueryFilters));


        protected readonly QueryCompilationContext QueryCompilationContext;
        protected readonly AbpGlobalFiltersOptionsExtension GlobalFiltersExtension;
        protected BasicTenantInfo CurrentTenant => GlobalFiltersExtension?.CurrentTenantAccessor?.Current;

        //private QuerySplittingBehavior? _querySplittingBehavior { get; set; } = null;
        private readonly List<MethodCallExpression> _includeExpressionCache = new();
        private readonly HashSet<string> _parameterNames = new();

        public AbpFilterAppendingExpressionVisitor(
            [NotNull] QueryCompilationContext queryCompilationContext)
        {
            QueryCompilationContext = queryCompilationContext;

            GlobalFiltersExtension = QueryCompilationContext.ContextOptions
                .FindExtension<AbpGlobalFiltersOptionsExtension>();

            //if (queryCompilationContext is RelationalQueryCompilationContext)
            //{
            //    _querySplittingBehavior = RelationalOptionsExtension.Extract(queryCompilationContext.ContextOptions)?.QuerySplittingBehavior;
            //}
        }

        // todo: cache 'Include'/'ThenInclude' statements not complete!
        protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
        {
            // Stop application of filters if they have already been applied
            if (QueryCompilationContext.Tags.Contains(IgnoreAbpQueryFiltersTag)
                || QueryCompilationContext.Tags.Contains(AbpQueryFiltersAppliedTag))
            {
                return base.VisitMethodCall(methodCallExpression);
            }

            Check.NotNull(methodCallExpression, nameof(methodCallExpression));

            // Handle 'IgnoreAbpQueryFilters()' method calls
            if (methodCallExpression.Method.DeclaringType == typeof(AbpQueryableExtensions_DemoProj)
                && methodCallExpression.Method.IsGenericMethod
                && ExtractAbpQueryFilterMetadata(methodCallExpression) is Expression expression)
            {
                GlobalFiltersExtension.SetAbpQueryFiltersDisabled();
                return expression;
            }

            // Extract 'Include(...)' expression information
            if (methodCallExpression.Method.DeclaringType == typeof(Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions)
                && methodCallExpression.Method.IsGenericMethod
                && (methodCallExpression.Method.Name == nameof(EntityFrameworkQueryableExtensions.Include)
                    //|| methodCallExpression.Method.Name == nameof(EntityFrameworkQueryableExtensions.ThenInclude)
                    ))
            {
                _includeExpressionCache.Add(methodCallExpression);
            }

            return base.VisitMethodCall(methodCallExpression);
        }

        // TODO: Make sure that this method emulates the functionality in AbpDbContext.ConfigureGlobalFilters
        protected override Expression VisitExtension(Expression extensionExpression)
        {
            // Stop application of filters if they have already been applied or 'IgnoreAbpQueryFilters()' has been requested
            if (QueryCompilationContext.Tags.Contains(IgnoreAbpQueryFiltersTag)
                || QueryCompilationContext.Tags.Contains(AbpQueryFiltersAppliedTag))
            {
                return base.VisitExtension(extensionExpression);
            }

            // Only append the additional 'Where'/'Include' clauses to the query if we are at the root expression
            // otherwise we might insert them in an invalid position which will cause exceptions
            // This modified query will then be passed to EF Core's query processor for further processing.
            if (extensionExpression is QueryRootExpression queryRootExpression)
            {
                var modifiedQuery = extensionExpression;
                var processedCount = 0;

                // Apply filters to root entity
                processedCount += ApplyAbpGlobalFilters(ref modifiedQuery, queryRootExpression);

                // Apply filters to related entities only if 'Include(...)' was used on the query
                if (AbpQueryFilterDemoConsts.ApplyFiltersToNavigations && !QueryCompilationContext.IgnoreAutoIncludes
                    // todo: this is probably the wrong thing to do - review it!
                    //&& _querySplittingBehavior != QuerySplittingBehavior.SplitQuery
                    )
                {
                    foreach (var childEntity in queryRootExpression.EntityType.GetNavigations())
                    {
                        if (childEntity == null) continue;

                        processedCount += ApplyAbpGlobalFilters(ref modifiedQuery, queryRootExpression, childEntity);
                    }
                }

                QueryCompilationContext.Tags.Add(AbpQueryFiltersAppliedTag);

                return Visit(modifiedQuery);
            }

            return base.VisitExtension(extensionExpression);
        }

        // Because we know the filters we are creating at compile-time this method can be reasonably simple...
        // However, this method may not cover all scenarios and may need to emulate the 'ApplyQueryFilters' more closely
        // see: https://github.com/dotnet/efcore/blob/f54b9dcd189c91fc4b01b79c9387d23095819a8f/src/EFCore/Query/Internal/NavigationExpandingExpressionVisitor.cs#L1412-L1462
        // see other: https://github.com/dotnet/efcore/blob/f54b9dcd189c91fc4b01b79c9387d23095819a8f/src/EFCore/Query/Internal/NavigationExpandingExpressionVisitor.cs#L844
        // see other: https://github.com/dotnet/efcore/blob/da00fb69d615fa22a83dfee2077ad31b7bd15823/src/EFCore.Relational/Query/QuerySqlGenerator.cs#L979-L1002
        /* TODO:
         * - Optimise so filters are not applied if there is a 'Where'/'Include' clause that already satisfies the filter (or if a filter will conflict with a where statement?? i.e. IsDeleted==true && IsDeleted==false)
         * - Optimise so filters are not applied if the query doesn't 'Include' (eager load) any related entities AND there is no Explicit/Lazy loading (i.e. Where clause on related entity)
         * - Support custom DataFilters?
         * - Possibly handle when 'RelationalQueryableExtensions.AsSingleQuery()' and 'RelationalQueryableExtensions.AsSplitQuery()' are used
         * - Test complex scenarios
         *      - Test if the DataFilter and CurrentTenant are always available and correctly scoped (multiple DbContexts, using migrations etc)
         *      - Select statement, anonymous returns, abstract base classes, TPH/TPC inheritence, shadow properties etc.
         */
        protected virtual int ApplyAbpGlobalFilters(ref Expression sourceQuery, in QueryRootExpression queryRootExpression, INavigation targetEntity = null)
        {
            // Don't want to apply filters to an abstract class
            // todo: check if this is appropriate
            if (queryRootExpression.EntityType.BaseType != null && targetEntity == null)
            {
                return 0;
            }

            List<Expression> expressionCache = new();

            var sourceEntityType = queryRootExpression.EntityType;
            var sourceParam = Expression.Parameter(sourceEntityType.ClrType, GetParamName(sourceEntityType, _parameterNames));

            IEntityType targetEntityType = targetEntity == null ? sourceEntityType : targetEntity.TargetEntityType;

            // Apply ISoftDelete filter
            if (typeof(ISoftDelete).IsAssignableFrom(targetEntityType.ClrType))
            {
                // todo: Update this after IDataFilter and ISoftDelete contain appropriate generic interfaces
                var entitySoftDelete = GetEntityFilter(typeof(ISoftDelete<>), targetEntityType.ClrType);
                var softDelete = GetEntityFilter(typeof(ISoftDelete));
                // Filters targetted at specific entities take priority over general filters
                if (entitySoftDelete.IsSet && entitySoftDelete.IsEnabled || !entitySoftDelete.IsSet && (!softDelete.IsSet || softDelete.IsEnabled))
                {
                    var softDeleteExpr =
                        EnsureCollectionWrapped(param =>
                            // !e.Blog.IsDeleted
                            Expression.Not(
                                // e.Blog.IsDeleted
                                Expression.MakeMemberAccess(
                                    // e.Blog
                                    param,
                                    //todo: can we use targetEntity.TargetEntityType.FindProperty() method?
                                    Expression.Property(param, "IsDeleted").Member
                                )
                            ),
                            sourceQuery,
                            sourceParam,
                            _parameterNames,
                            _includeExpressionCache,
                            targetEntity);

                    if (softDeleteExpr is MethodCallExpression)
                    {
                        sourceQuery = softDeleteExpr;
                    }
                    else
                    {
                        expressionCache.Add(softDeleteExpr);
                    }
                }
            }

            // Apply IMultiTenant filter
            if (CurrentTenant != null && typeof(IMultiTenant).IsAssignableFrom(targetEntityType.ClrType))
            {
                // todo: Update this after IDataFilter and IMultiTenant contain appropriate generic interfaces
                var entityMultiTenant = GetEntityFilter(typeof(IMultiTenant<>), targetEntityType.ClrType);
                var multiTenant = GetEntityFilter(typeof(IMultiTenant));
                // Filters targetted at specific entities take priority over general filters
                if (entityMultiTenant.IsSet && entityMultiTenant.IsEnabled || !entityMultiTenant.IsSet && (!multiTenant.IsSet || multiTenant.IsEnabled))
                {
                    var tenantIdExpr =
                        EnsureCollectionWrapped(param =>
                            // x.Blog.TenantId == "GUID"
                            // todo: a 'Convert' expression might need to wrap the 'Equal' expression?
                            Expression.Equal(
                                // x.Blog.TenantId
                                Expression.MakeMemberAccess(
                                    // x.Blog
                                    param,
                                    Expression.Property(param, "TenantId").Member //tenantIdMember
                                ),
                                Expression.Constant(CurrentTenant.TenantId)
                            ),
                            sourceQuery,
                            sourceParam,
                            _parameterNames,
                            _includeExpressionCache,
                            targetEntity);

                    if (tenantIdExpr is MethodCallExpression mce)
                    {
                        //if (mce.Method == QueryableExtensions.IncludeMethodInfo)
                        //{
                        sourceQuery = tenantIdExpr;
                        //}
                    }
                    else
                    {
                        expressionCache.Add(tenantIdExpr);
                    }
                }
            }

            // Combine all simple (non-MethodCall) filters to simplify the expression
            if (expressionCache.Count > 0)
            {
                var combinedExpression = expressionCache.Aggregate((left, right) => Expression.AndAlso(left, right));

                sourceQuery = Expression.Call(
                    QueryableMethods.Where.MakeGenericMethod(sourceEntityType.ClrType),
                    sourceQuery,
                    Expression.Quote(Expression.Lambda(combinedExpression, sourceParam)));
            }

            return expressionCache.Count;
        }

        // todo: We are enforcing eager loding of collections here which might not be wanted! But if the query has 'Include' for this collection then we are safe to do this.
        // todo: This doesn't actually filter collections because we need to call 'Select' instead of 'Where' e.g. 'blogQuery.Select(e => e.Posts.Where(p => !p.IsDeleted)))
        protected static Expression EnsureCollectionWrapped(
            Func<Expression, Expression> expressionTemplate,
            in Expression sourceQuery,
            ParameterExpression sourceParam,
            HashSet<string> parameterNames,
            in List<MethodCallExpression> includeExpressionCache,
            INavigation targetEntity = null)
        {
            if (targetEntity != null && targetEntity.IsCollection)
            {
                // blog.Posts
                Expression targetExpr = Expression.MakeMemberAccess(sourceParam, targetEntity.PropertyInfo);

                var collectionParam = Expression.Parameter(
                    targetEntity.TargetEntityType.ClrType,
                    GetParamName(targetEntity.TargetEntityType, parameterNames)
                );

                // 1. Get the expression template with the collection as the target parameter
                var innerExpression = expressionTemplate.Invoke(collectionParam);

                // 2. Append AsQueryable() to ensure collection isn't loaded to memory
                targetExpr =
                    Expression.Call(
                        instance: null,
                        method: QueryableMethods.AsQueryable.MakeGenericMethod(targetEntity.TargetEntityType.ClrType),
                        arguments: new[] { targetExpr }
                    );


                // 3. Wrap the expression template in an Where() call
                targetExpr =
                    // blog.Posts.AsQueryable().Where(post => !post.IsDeleted)
                    Expression.Call(
                        instance: null,
                        method: QueryableMethods.Where.MakeGenericMethod(targetEntity.TargetEntityType.ClrType),
                        arguments: new[]
                        { 
                            // blog.Posts.AsQueryable()
                            targetExpr,
                            // post => !post.IsDeleted
                            Expression.Quote(
                                Expression.Lambda(
                                    // !post.IsDeleted
                                    innerExpression,
                                    // post
                                    collectionParam
                                )
                            )
                        }
                    );

                // 4. Wrap the Where() call in a Include() call - creating a filtered include statement

                MethodInfo includeMethodInfo = QueryableExtensions.IncludeMethodInfo
                            .MakeGenericMethod(sourceParam.Type, ((MethodCallExpression)targetExpr).Method.ReturnType);

                //var found = includeExpressionCache.TryGetValue(includeMethodInfo, out var lambdaExpression);
                MethodCallExpression methodCallExpression = null;

                //foreach (var expression in includeExpressionCache)
                //{
                //    //expression.ReturnType == targetEntity.TargetEntityType.ClrType

                //    var lambda = expression.Arguments[1].UnwrapLambdaFromQuote();

                //    if (lambda.Body is ConstantExpression includeConstant)
                //    {
                //        if (includeConstant.Value is string navigationChain)
                //        {
                //            var navigationPaths = navigationChain.Split(new[] { "." }, StringSplitOptions.None);

                //            if (navigationPaths.Length > 0 && targetEntity.Name == navigationPaths[0])
                //            {

                //            }
                //        }
                //    }
                //}

                var methodCallLambda =
                    Expression.Quote(
                        Expression.Lambda(
                            // blog.Posts.AsQueryable().Where(post => !post.IsDeleted)
                            targetExpr,
                            // blog
                            sourceParam
                        )
                    );

                // .Include(blog => blog.Posts.AsQueryable().Where(post => !post.IsDeleted))
                return methodCallExpression != null
                    ? methodCallExpression.Update(methodCallExpression.Object, new[] { methodCallLambda })
                    : Expression.Call(
                        instance: null,
                        method: includeMethodInfo,
                        arguments: new[]
                        {
                            // blog
                            (Expression)sourceQuery,
                            // blog => blog.Posts.AsQueryable().Where(post => !post.IsDeleted)
                            methodCallLambda
                        }
                    );

            }
            // For non-collection navigations, just return the original expression
            // todo: use filtered includes for non-collection navigations?
            else
            {
                return expressionTemplate.Invoke(targetEntity == null
                    ? sourceParam
                    : Expression.MakeMemberAccess(sourceParam, targetEntity.PropertyInfo)
                );
            }
        }

#nullable enable
        private Expression? ExtractAbpQueryFilterMetadata(MethodCallExpression methodCallExpression)
        {
            // We visit innerQueryable first so that we can get information in the same order operators are applied.
            var genericMethodDefinition = methodCallExpression.Method.GetGenericMethodDefinition();

            if (genericMethodDefinition == IgnoreAbpQueryFiltersMethodInfo)
            {
                QueryCompilationContext.AddTag(IgnoreAbpQueryFiltersTag);

                // Remove 'IgnoreAbpQueryFilters' tag from the queryable
                // todo: is a fullly recursive 'Visit' call required?
                return Visit(methodCallExpression.Arguments[0]);
            }

            return null;
        }
#nullable disable

        // see: https://github.com/dotnet/efcore/blob/f54b9dcd189c91fc4b01b79c9387d23095819a8f/src/EFCore/Query/Internal/NavigationExpandingExpressionVisitor.cs#L1625
        protected static string GetParamName(IEntityType entityType, HashSet<string> existingParamNames)
        {
            var uniqueName = entityType.ShortName()[0].ToString().ToLowerInvariant();
            var index = 0;

            while (existingParamNames.Contains(uniqueName))
            {
                uniqueName = $"{uniqueName}{index++}";
            }

            existingParamNames.Add(uniqueName);

            return uniqueName;
        }
#nullable enable
        protected (bool IsSet, bool IsEnabled) GetEntityFilter(Type filterType, Type? entityType = null)
        {
            var type = entityType == null ? filterType : filterType.MakeGenericType(entityType);

            if (GlobalFiltersExtension?.FilterCollection != null && GlobalFiltersExtension.FilterCollection.TryGetValue(type, out var filter))
            {
                var propertyInfo = filter.GetType().GetProperty("IsEnabled", BindingFlags.Public | BindingFlags.Instance);
                if (propertyInfo != null)
                {
                    return (true, ((bool?)propertyInfo.GetValue(filter)) ?? false);
                }
            }

            // Based on DataFilter.EnsureInitialized() method.
            // Return 'false' if there is not default state for specific entities, but return true for generic filters.
            return (false, GlobalFiltersExtension?.DataFilterOptions.DefaultStates.GetOrDefault(type)?.IsEnabled ?? entityType != null);
        }
#nullable disable
    }
}
