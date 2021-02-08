using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Volo.Abp;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.MultiTenancy;

// Namespace should be 'Volo.Abp.EntityFrameworkCore.MySQL' according to: https://docs.microsoft.com/en-us/ef/core/providers/writing-a-provider#suggested-naming-of-third-party-providers
namespace AbpQueryFilterDemo.EntityFrameworkCore
{
    public abstract class CustomAbpDbContext<TDbContext> : AbpDbContext<TDbContext> where TDbContext : DbContext
    {
        private bool USECUSTOMFILTERING = true;
        protected CustomAbpDbContext(DbContextOptions<TDbContext> options) : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            optionsBuilder.ReplaceService<IQueryTranslationPreprocessorFactory, CustomQueryTranslationPreprocessorFactory>();

            // todo: LazyServiceProvider is null when using powershell 'add-migration' etc. this needs investigating!
            if (USECUSTOMFILTERING && LazyServiceProvider != null)
            {
                // Custom Extension to acces DataFilter and CurrentTenant 
                // todo: does this work okay when the when the IServiceProvider is changed within the query context?
                var extension = optionsBuilder.Options.FindExtension<AbpGlobalFiltersOptionsExtension>()
                    ?? new AbpGlobalFiltersOptionsExtension(
                        this.DataFilter, 
                        LazyServiceProvider.LazyGetRequiredService<IOptions<AbpDataFilterOptions>>(), 
                        this.CurrentTenant);

                ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);
            }
        }

        [Obsolete]
        protected override void ConfigureGlobalFilters<TEntity>(ModelBuilder modelBuilder, IMutableEntityType mutableEntityType)
            where TEntity : class
        {
            if (USECUSTOMFILTERING) return;

            base.ConfigureGlobalFilters<TEntity>(modelBuilder, mutableEntityType);
        }

        [Obsolete]
        protected override Expression<Func<TEntity, bool>> CreateFilterExpression<TEntity>()
           where TEntity : class
        {
            if (USECUSTOMFILTERING) return null; // DISABLE
            
            return base.CreateFilterExpression<TEntity>();
        }
    }

    // Original from: https://github.com/dotnet/efcore/blob/b8483772f298f5ada8b2b5253a9904c93c34919f/test/EFCore.Tests/ServiceProviderCacheTest.cs#L226-L264
    public class AbpGlobalFiltersOptionsExtension : IDbContextOptionsExtension
    {
        public DbContextOptionsExtensionInfo Info => _info ??= new ExtensionInfo(this);
        private DbContextOptionsExtensionInfo _info;

        public IDataFilter DataFilter { get; } = null;
        public AbpDataFilterOptions DataFilterOptions { get; } = new AbpDataFilterOptions();
        public ICurrentTenant CurrentTenant { get; } = null;

        public AbpGlobalFiltersOptionsExtension(IDataFilter dataFilter, IOptions<AbpDataFilterOptions> filterOptions, ICurrentTenant currentTenant)
        {
            DataFilter = dataFilter;
            DataFilterOptions = filterOptions.Value;
            CurrentTenant = currentTenant;
        }

        public virtual void ApplyServices(IServiceCollection services) { }
        public virtual void Validate(IDbContextOptions options) { }

        private sealed class ExtensionInfo : DbContextOptionsExtensionInfo
        {
            public ExtensionInfo(IDbContextOptionsExtension extension) : base(extension) { }
            public override bool IsDatabaseProvider => false;
            public override long GetServiceProviderHashCode() => 0;
            // todo: list more debug info (i.e. tenant info and data filters) in log output
            public override string LogFragment => "Using AbpGlobalFilters";
            public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
            {
                debugInfo["AbpGlobalFilters"] = "1";
            }
        }
    }


    // Based on Microsoft.EntityFrameworkCore.Query.Internal.QueryTranslationPreprocessorFactory
    // see: https://github.com/dotnet/efcore/blob/46996600cb3f152e3e21ee4d07effdc516dbf4e9/src/EFCore/Query/Internal/QueryTranslationPreprocessorFactory.cs
    public class CustomQueryTranslationPreprocessorFactory : IQueryTranslationPreprocessorFactory
    {
        protected QueryTranslationPreprocessorDependencies Dependencies { get; }
        protected RelationalQueryTranslationPreprocessorDependencies RelationalDependencies { get; }

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

    // Based on RelationalQueryTranslationPreprocessor
    // see: https://github.com/dotnet/efcore/blob/43a5493251faf2591eb2f50abef4b8597374642f/src/EFCore.Relational/Query/RelationalQueryTranslationPreprocessor.cs
    // and see: https://github.com/dotnet/efcore/blob/b8483772f298f5ada8b2b5253a9904c93c34919f/src/EFCore.Relational/Infrastructure/EntityFrameworkRelationalServicesBuilder.cs#L177
    /// <summary>
    /// A class that preprocesses the query before translation.
    /// </summary>
    public class CustomQueryTranslationPreprocessor : RelationalQueryTranslationPreprocessor
    {
        protected AbpGlobalFiltersOptionsExtension GlobalFiltersExtension;

        public CustomQueryTranslationPreprocessor(
            [NotNull] QueryTranslationPreprocessorDependencies dependencies,
            [NotNull] RelationalQueryTranslationPreprocessorDependencies relationalDependencies,
            [NotNull] QueryCompilationContext queryCompilationContext)
            : base(dependencies, relationalDependencies, queryCompilationContext)
        {
            GlobalFiltersExtension = QueryCompilationContext.ContextOptions
                .FindExtension<AbpGlobalFiltersOptionsExtension>();
        }

        // Called once per query
        // see: https://github.com/dotnet/efcore/blob/46996600cb3f152e3e21ee4d07effdc516dbf4e9/src/EFCore/Query/QueryTranslationPreprocessor.cs#L55-L69
        public override Expression Process(Expression query)
        {
            if (GlobalFiltersExtension != null && GlobalFiltersExtension.DataFilter != null)
            {
                query = new AppendAbpFiltersExpressionVisitor(QueryCompilationContext, in GlobalFiltersExtension)
                   .Visit(query);
            }

            var q = base.Process(query);
            return q;
        }

        public override Expression NormalizeQueryableMethod(Expression expression)
        {
            var query = base.NormalizeQueryableMethod(expression);
            
            return query;
        }
    }

    public class AppendAbpFiltersExpressionVisitor : ExpressionVisitor
    {
        internal static readonly string AbpQueryFiltersAppliedTag = "AbpQueryFiltersApplied";

        internal static readonly string IgnoreAbpQueryFiltersTag = nameof(AbpQueryableExtensions_DemoProj.IgnoreAbpQueryFilters);

        internal static readonly MethodInfo IgnoreAbpQueryFiltersMethodInfo
            = typeof(AbpQueryableExtensions_DemoProj)
                .GetTypeInfo()
                .GetDeclaredMethod(nameof(AbpQueryableExtensions_DemoProj.IgnoreAbpQueryFilters));

        protected IDataFilter DataFilter => GlobalFiltersExtension?.DataFilter;
        protected AbpDataFilterOptions DataFilterOptions => GlobalFiltersExtension?.DataFilterOptions;
        protected ICurrentTenant CurrentTenant => GlobalFiltersExtension?.CurrentTenant;

        // todo: Remove this after IDataFilter is updated to expose the raw values
        // todo: This will not work on medium-trust environments! https://stackoverflow.com/a/96020/2634818
        //       The sooner we can stop using this, the better!
        protected System.Collections.Concurrent.ConcurrentDictionary<Type, object> DataFilterCollection => 
            DataFilter == null ? null : (_cachedDataFilterCollection == null ? _cachedDataFilterCollection = typeof(DataFilter)
                .GetField("_filters", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(DataFilter) as System.Collections.Concurrent.ConcurrentDictionary<Type, object>
            : _cachedDataFilterCollection);
        private System.Collections.Concurrent.ConcurrentDictionary<Type, object> _cachedDataFilterCollection;

        protected readonly QueryCompilationContext QueryCompilationContext;
        protected readonly AbpGlobalFiltersOptionsExtension GlobalFiltersExtension;

        public AppendAbpFiltersExpressionVisitor(
            [NotNull] QueryCompilationContext queryCompilationContext,
            [NotNull] in AbpGlobalFiltersOptionsExtension globalFiltersExtension)
        {
            QueryCompilationContext = queryCompilationContext;
            GlobalFiltersExtension = globalFiltersExtension;
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

            // Handle 'IgnorAbpQueryFilters()' method calls
            if (methodCallExpression.Method.DeclaringType == typeof(AbpQueryableExtensions_DemoProj)
                && methodCallExpression.Method.IsGenericMethod
                && ExtractAbpQueryFilterMetadata(methodCallExpression) is Expression expression)
            {
                return expression;
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

            // Only append the additional 'Where' clauses to the query if we are at the root expression
            // otherwise we might insert them in an invalid position which will cause exceptions
            // This modified query will then be passed to EF Core's query processor for further processing.
            if (extensionExpression is QueryRootExpression queryRootExpression)
            {
                var modifiedQuery = extensionExpression;
                var processedCount = 0;

                // Apply filters to root entity
                processedCount += ApplyAbpGlobalFilters(ref modifiedQuery, queryRootExpression.EntityType);

                // Apply filters to related entities
                if (!QueryCompilationContext.IgnoreAutoIncludes)
                {
                    foreach (var childEntity in queryRootExpression.EntityType.GetNavigations())
                    {
                        if (childEntity == null) continue;

                        processedCount += ApplyAbpGlobalFilters(ref modifiedQuery, queryRootExpression.EntityType, childEntity);
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
         * - Optimise so filters are not applied if there is a 'Where' caluse that already satisfies the filter (or if a filter will conflict with a where statement?? i.e. IsDeleted==true && IsDeleted==false)
         * - Optimise so filters are not applied if the query doesn't 'Include' (eager load) any related entities AND there is no Explicit/Lazy loading (i.e. Where clause on related entity)
         * - Support custom DataFilters?
         * - Handle when 'RelationalQueryableExtensions.AsSingleQuery()' and 'RelationalQueryableExtensions.AsSplitQuery()' are used
         * - Test complex scenarios
         *      - Test if the DataFilter and CurrentTenant are always available and correctly scoped (multiple DbContexts, using migrations etc)
         *      - Select statement, anonymous returns, abstract base classes, TPH/TPC inheritence, shadow properties etc.
         */
        protected virtual int ApplyAbpGlobalFilters(ref Expression sourceQuery, IEntityType sourceEntityType, INavigation targetEntity = null)
        {
            // Don't want to apply filters to an abstract class
            // todo: check if this is appropriate
            if (sourceEntityType.BaseType != null && targetEntity == null)
            {
                return 0;
            }

            List<Expression> expressionCache = new();
            var whereMethodInfo = QueryableMethods.Where.MakeGenericMethod(sourceEntityType.ClrType);
            var sourceParam = Expression.Parameter(sourceEntityType.ClrType, GetParamName(sourceEntityType.ClrType));

            IEntityType targetEntityType = targetEntity == null ? sourceEntityType : targetEntity.TargetEntityType;

            // Apply ISoftDelete filter
            if (typeof(ISoftDelete).IsAssignableFrom(targetEntityType.ClrType))
            {
                // note: this doesn't work because the call to 'IsEnabled' creates an entry which resolves automatically to 'true'
                //var entitySoftDeleteEnabled = (bool)typeof(DataFilter)
                //    .GetMethod(nameof(DataFilter.IsEnabled))
                //    .MakeGenericMethod(typeof(ISoftDelete<>).MakeGenericType(targetEntityType.ClrType))
                //    .Invoke(DataFilter, null);

                // todo: Update this after IDataFilter and ISoftDelete contain appropriate generic interfaces
                var entitySoftDelete = GetEntityFilter(typeof(ISoftDelete<>), targetEntityType.ClrType);
                var softDeleteEnabled = DataFilter.IsEnabled<ISoftDelete>();
                // Filters targetted at specific entities take priority over general filters
                if (entitySoftDelete.IsSet && entitySoftDelete.IsEnabled || !entitySoftDelete.IsSet && softDeleteEnabled)
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
                            sourceParam,
                            targetEntity);

                    // !x.Blog.IsDeleted
                    expressionCache.Add(softDeleteExpr);
                }
            }

            // Apply IMultiTenant filter
            //if (targetEntityType.ClrType.GetInterface(nameof(IMultiTenant)) != null))
            if (typeof(IMultiTenant).IsAssignableFrom(targetEntityType.ClrType))
            {
                // note: this doesn't work because the call to 'IsEnabled' creates an entry which resolves automatically to 'true'
                //var entityMultiTenantEnabled = (bool)typeof(DataFilter)
                //    .GetMethod(nameof(DataFilter.IsEnabled))
                //    .MakeGenericMethod(typeof(IMultiTenant<>).MakeGenericType(targetEntityType.ClrType))
                //    .Invoke(DataFilter, null);

                // todo: Update this after IDataFilter and IMultiTenant contain appropriate generic interfaces
                var entityMultiTenant = GetEntityFilter(typeof(IMultiTenant<>), targetEntityType.ClrType);
                var multiTenantEnabled = DataFilter.IsEnabled<IMultiTenant>();
                // Filters targetted at specific entities take priority over general filters
                if (entityMultiTenant.IsSet && entityMultiTenant.IsEnabled || !entityMultiTenant.IsSet && multiTenantEnabled)
                {
                    // todo: a 'Convert' expression might need to wrap the 'Equal' expression?
                    var tenantIdExpr =
                        EnsureCollectionWrapped(param =>
                            // x.Blog.TenantId == "GUID"
                            Expression.Equal(
                                // x.Blog.TenantId
                                Expression.MakeMemberAccess(
                                    // x.Blog
                                    param,
                                    Expression.Property(param, "TenantId").Member //tenantIdMember
                                ),
                                Expression.Constant(CurrentTenant.Id)
                            ),
                            sourceParam,
                            targetEntity);

                    // x.Blog.TenantId == "GUID"
                    expressionCache.Add(tenantIdExpr);
                }
            }

            // Combine all filters to simplify the expression
            if (expressionCache.Count > 0)
            {
                var combinedExpression = expressionCache.Aggregate((left, right) => Expression.AndAlso(left, right));

                sourceQuery = Expression.Call(
                    whereMethodInfo,
                    sourceQuery,
                    Expression.Quote(Expression.Lambda(combinedExpression, sourceParam)));
            }
            
            return expressionCache.Count;
        }

        // todo: We are enforcing eager loding of collections here which might not be wanted! But if the query has 'Include' for this collection then we are safe to do this.
        // todo: This doesn't actually filter collections because we need to call 'Select' instead of 'Where' e.g. 'blogQuery.Select(e => e.Posts.Where(p => !p.IsDeleted)))
        protected static Expression EnsureCollectionWrapped(
            Func<Expression, Expression> expressionTemplate,
            ParameterExpression sourceParam,
            INavigation targetEntity = null)
        {
            if (targetEntity != null && targetEntity.IsCollection)
            {
                Expression targetExpr = Expression.MakeMemberAccess(sourceParam, targetEntity.PropertyInfo);

                var collectionParam = Expression.Parameter(
                    targetEntity.TargetEntityType.ClrType,
                    GetParamName(targetEntity.TargetEntityType.ClrType)
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

                // 3. Wrap the expression template in an Any() call
                return
                    // e.Blogs.AsQueryable().Any(c0 => !c0.IsDeleted)
                    Expression.Call(
                        instance: null,
                        // note: Use this version when the source is IEnumerable
                        //typeof(Enumerable).GetMethods().Single(mi => mi.Name == "Any" && mi.GetParameters().Count() == 2)
                        method: QueryableMethods.AnyWithPredicate.MakeGenericMethod(targetEntity.TargetEntityType.ClrType),
                        arguments: new[]
                        { 
                            // e.Blogs.AsQueryable()
                            targetExpr,
                            // c0 => !c0.IsDeleted
                            Expression.Lambda(
                                // !c0.IsDeleted
                                innerExpression,
                                // c0
                                collectionParam
                            )
                        }
                    );
            }
            // For non-collection navigations, just return the original expression
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

        protected static string GetParamName(Type clrType) => char.ToLowerInvariant(clrType.Name[0]).ToString();

        protected (bool IsSet, bool IsEnabled) GetEntityFilter(Type filterType, Type entityType)
        {
            var type = filterType.MakeGenericType(entityType);

            if (DataFilterCollection != null && DataFilterCollection.TryGetValue(type, out var filter))
            {
                return (true, (bool)filter.GetType()
                    .GetProperty("IsEnabled", BindingFlags.Public | BindingFlags.Instance)
                    .GetValue(filter));
            }

            // Based on DataFilter.EnsureInitialized() method.
            // Because we are querying a specific entity state, we should return 'false' if there is not default state for this entity.
            return (false, DataFilterOptions?.DefaultStates.GetOrDefault(type)?.IsEnabled ?? false);
        }
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
