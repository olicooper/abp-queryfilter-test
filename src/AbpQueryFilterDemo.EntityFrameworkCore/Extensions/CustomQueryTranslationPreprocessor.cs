using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace AbpQueryFilterDemo.EntityFrameworkCore
{
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
        private readonly RelationalQueryCompilationContext _relationalQueryCompilationContext;

        public CustomQueryTranslationPreprocessor(
            [NotNull] QueryTranslationPreprocessorDependencies dependencies,
            [NotNull] RelationalQueryTranslationPreprocessorDependencies relationalDependencies,
            [NotNull] QueryCompilationContext queryCompilationContext)
            : base(dependencies, relationalDependencies, queryCompilationContext)
        {
            _relationalQueryCompilationContext = (RelationalQueryCompilationContext)queryCompilationContext;
        }

        // Called once per query
        // see: https://github.com/dotnet/efcore/blob/46996600cb3f152e3e21ee4d07effdc516dbf4e9/src/EFCore/Query/QueryTranslationPreprocessor.cs#L55-L69
        public override Expression Process(Expression query)
        {
            if (AbpQueryFilterDemoConsts.UseCustomFiltering)
            {
                // *+*+*+*+* This is where the magic happens *+*+*+*+*
                query = new AbpFilterAppendingExpressionVisitor(QueryCompilationContext).Visit(query);
            }

            if (AbpQueryFilterDemoConsts.ExposePreprocessorProcessMethods)
            {
#pragma warning disable EF1001 // Internal EF Core API usage.

                // from QueryTranslationPreprocessor
                query = new Microsoft.EntityFrameworkCore.Query.Internal.InvocationExpressionRemovingExpressionVisitor().Visit(query);
                query = NormalizeQueryableMethod(query);
                query = new Microsoft.EntityFrameworkCore.Query.Internal.NullCheckRemovingExpressionVisitor().Visit(query);
                query = new Microsoft.EntityFrameworkCore.Query.Internal.SubqueryMemberPushdownExpressionVisitor(QueryCompilationContext.Model).Visit(query);
                query = new Microsoft.EntityFrameworkCore.Query.Internal.NavigationExpandingExpressionVisitor(this, QueryCompilationContext, Dependencies.EvaluatableExpressionFilter).Expand(query);
                query = new Microsoft.EntityFrameworkCore.Query.Internal.QueryOptimizingExpressionVisitor().Visit(query);
                query = new Microsoft.EntityFrameworkCore.Query.Internal.NullCheckRemovingExpressionVisitor().Visit(query);

                // from RelationalQueryTranslationPreprocessor
                query = _relationalQueryCompilationContext.QuerySplittingBehavior == QuerySplittingBehavior.SplitQuery
                    ? new Microsoft.EntityFrameworkCore.Query.Internal.SplitIncludeRewritingExpressionVisitor().Visit(query)
                    : query;

#pragma warning restore EF1001 // Internal EF Core API usage.
            }
            else
            {
                query = base.Process(query);
            }

            return query;
        }
    }
}
