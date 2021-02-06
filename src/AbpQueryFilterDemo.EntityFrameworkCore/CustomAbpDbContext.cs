using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using Volo.Abp;
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
            if (LazyServiceProvider != null)
            {
                // Custom Extension to acces DataFilter and CurrentTenant 
                // todo: does this work okay when the when the IServiceProvider is changed within the query context?
                var extension = optionsBuilder.Options.FindExtension<AbpGlobalFiltersOptionsExtension>()
                    ?? new AbpGlobalFiltersOptionsExtension(this.DataFilter, this.CurrentTenant);

                ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);
            }
        }

        [Obsolete]
        protected override void ConfigureGlobalFilters<TEntity>(ModelBuilder modelBuilder, IMutableEntityType mutableEntityType)
            where TEntity : class
        {
            return;
        }

        [Obsolete]
        protected override Expression<Func<TEntity, bool>> CreateFilterExpression<TEntity>()
           where TEntity : class
        {
            return null; // DISABLE
        }
    }

    // Original from: https://github.com/dotnet/efcore/blob/b8483772f298f5ada8b2b5253a9904c93c34919f/test/EFCore.Tests/ServiceProviderCacheTest.cs#L226-L264
    public class AbpGlobalFiltersOptionsExtension : IDbContextOptionsExtension
    {
        public DbContextOptionsExtensionInfo Info => _info ??= new ExtensionInfo(this);
        private DbContextOptionsExtensionInfo _info;

        public IDataFilter DataFilter { get; } = null;
        public ICurrentTenant CurrentTenant { get; } = null;

        public AbpGlobalFiltersOptionsExtension(IDataFilter dataFilter, ICurrentTenant currentTenant)
        {
            DataFilter = dataFilter;
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
        protected QueryTranslationPreprocessorDependencies Dependencies;
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
    }

    public class AppendAbpFiltersExpressionVisitor : ExpressionVisitor
    {
        public static readonly string AbpGlobalFiltersAppended = "AbpGlobalFiltersAppended";

        protected IDataFilter DataFilter => GlobalFiltersExtension?.DataFilter;
        protected ICurrentTenant CurrentTenant => GlobalFiltersExtension?.CurrentTenant;

        protected readonly QueryCompilationContext QueryCompilationContext;
        protected readonly AbpGlobalFiltersOptionsExtension GlobalFiltersExtension;

        public AppendAbpFiltersExpressionVisitor(
            [NotNull] QueryCompilationContext queryCompilationContext,
            [NotNull] in AbpGlobalFiltersOptionsExtension globalFiltersExtension)
        {
            QueryCompilationContext = queryCompilationContext;
            GlobalFiltersExtension = globalFiltersExtension;
        }

        // TODO: Make sure that this method emulates the functionality in AbpDbContext.ConfigureGlobalFilters
        protected override Expression VisitExtension(Expression extensionExpression)
        {
            // Stop application of filters if they have already been applied
            if (QueryCompilationContext.Tags.Contains(AbpGlobalFiltersAppended))
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
                if (ApplyAbpGlobalFilters(ref modifiedQuery, queryRootExpression.EntityType))
                {
                    ++processedCount;
                }

                // Apply filters to related entities
                foreach (var childEntity in queryRootExpression.EntityType.GetNavigations())
                {
                    if (childEntity == null) continue;

                    if (ApplyAbpGlobalFilters(ref modifiedQuery, queryRootExpression.EntityType, childEntity))
                    {
                        ++processedCount;
                    }

                }

                QueryCompilationContext.Tags.Add(AbpGlobalFiltersAppended);

                return Visit(modifiedQuery);
            }

            return base.VisitExtension(extensionExpression);
        }

        // Because we know the filters we are creating at compile-time this method can be reasonably simple...
        // However, this method may not cover all scenarios and may need to emulate the 'ApplyQueryFilters' more closely
        // see: https://github.com/dotnet/efcore/blob/f54b9dcd189c91fc4b01b79c9387d23095819a8f/src/EFCore/Query/Internal/NavigationExpandingExpressionVisitor.cs#L1412-L1462
        /* TODO:
         * - Optimise so filters are not applied if there is a 'Where' caluse that already satisfies the filter (or if a filter will conflict with a where statement?? i.e. IsDeleted==true && IsDeleted==false)
         * - Optimise so filters are not applied if the query doesn't 'Include' (eager load) any related entities AND there is no Explicit/Lazy loading (i.e. Where clause on related entity)
         * - Support custom DataFilters?
         * - Test complex scenarios
         *      - DataFilter and CurrentTenant are always available and correctly scoped
         *      - Select statement, anonymous returns, abstract base classes, TPH/TPC inheritence, shadow properties etc.
         */
        protected virtual bool ApplyAbpGlobalFilters(ref Expression sourceQuery, IEntityType sourceEntityType, INavigation targetEntity = null)
        {
            // Don't want to apply filters to an abstract class
            // todo: check if this is appropriate
            if (sourceEntityType.BaseType != null && targetEntity == null)
            {
                return false;
            }

            // todo: Allow processing/filtering of collections - the current filter brings back related 
            //       entities without any filtering! This differs from Microsoft's query filtering.
            if (targetEntity != null && targetEntity.IsCollection)
            {
                return false;
            }

            var hasAppliedAnyFilters = false;
            var whereExpr = QueryableMethods.Where.MakeGenericMethod(sourceEntityType.ClrType);
            var sourceParam = Expression.Parameter(sourceEntityType.ClrType, "x");

            IEntityType targetEntityType = targetEntity == null ? sourceEntityType : targetEntity.TargetEntityType;
            Expression targetExpr = targetEntity == null ? sourceParam : Expression.MakeMemberAccess(sourceParam, targetEntity.PropertyInfo);
            
            if (targetEntityType.ClrType.IsAssignableTo<ISoftDelete>())
            {
                // todo: Remove this method after IDataFilter and ISoftDelete contain appropriate generic interfaces
                var entitySoftDeleteEnabled = (bool)typeof(DataFilter)
                    .GetMethod(nameof(DataFilter.IsEnabled))
                    .MakeGenericMethod(typeof(ISoftDelete<>).MakeGenericType(targetEntityType.ClrType))
                    .Invoke(DataFilter, null);

                // Is ISoftDelete enabled for the specific entity OR for the general query
                if (DataFilter.IsEnabled<ISoftDelete>() && entitySoftDeleteEnabled || entitySoftDeleteEnabled)
                {
                    //if (targetType.GetMember("IsDeleted").Any())
                    //{
                        var softDeleteExpr =
                            // x => !x.Blog.IsDeleted
                            Expression.Lambda(
                                // !x.Blog.IsDeleted
                                Expression.Not(
                                    // x.Blog.IsDeleted
                                    Expression.MakeMemberAccess(
                                        // x.Blog
                                        targetExpr,
                                        //targetEntity.TargetEntityType.FindProperty
                                        Expression.Property(targetExpr, "IsDeleted").Member
                                    )
                                ),
                                sourceParam
                            );

                        // PostQuery.Where(x => !x.Blog.IsDeleted)
                        sourceQuery = Expression.Call(whereExpr, sourceQuery, softDeleteExpr);

                        hasAppliedAnyFilters = true;
                    //}
                }
            }

            if (targetEntityType.ClrType.IsAssignableTo<IMultiTenant>())
            {
                // todo: Remove this method after IDataFilter and IMultiTenant contain appropriate generic interfaces
                var entityMultiTenantEnabled = (bool)typeof(DataFilter)
                    .GetMethod(nameof(DataFilter.IsEnabled))
                    .MakeGenericMethod(typeof(IMultiTenant<>).MakeGenericType(targetEntityType.ClrType))
                    .Invoke(DataFilter, null);

                // Is IMultiTenant enabled for the specific entity OR for the general query
                if (DataFilter.IsEnabled<IMultiTenant>() && entityMultiTenantEnabled || entityMultiTenantEnabled)
                {
                    //var tenantIdMember = targetType.GetMember("TenantId").FirstOrDefault();
                    //if (tenantIdMember != null)
                    //{
                        // todo: a 'Convert' expression might need to wrap the 'Equal' expression?
                        var tenantIdExpr =
                            // x => x.Blog.TenantId == "GUID"
                            Expression.Lambda(
                                // x.Blog.TenantId == "GUID"
                                Expression.Equal(
                                    // x.Blog.TenantId
                                    Expression.MakeMemberAccess(
                                        // x.Blog
                                        targetExpr,
                                        Expression.Property(targetExpr, "TenantId").Member //tenantIdMember
                                    ),
                                    Expression.Constant(CurrentTenant.Id)
                                ),
                                sourceParam
                            );

                        // PostQuery.Where(x => x.Blog.TenantId == "GUID")
                        sourceQuery = Expression.Call(whereExpr, sourceQuery, tenantIdExpr);

                        hasAppliedAnyFilters = true;
                    //}
                }
            }

            return hasAppliedAnyFilters;
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
