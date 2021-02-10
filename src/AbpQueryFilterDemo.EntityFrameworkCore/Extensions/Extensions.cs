using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace AbpQueryFilterDemo.EntityFrameworkCore
{
    internal static class ExpressionExtensions
    {
        // https://github.com/dotnet/efcore/blob/da00fb69d615fa22a83dfee2077ad31b7bd15823/src/Shared/ExpressionExtensions.cs#L20
        public static LambdaExpression UnwrapLambdaFromQuote(this Expression expression)
            => (LambdaExpression)(expression is UnaryExpression unary && expression.NodeType == ExpressionType.Quote
                ? unary.Operand
                : expression);
    }

    internal static class QueryableExtensions
    {
        // see: https://github.com/dotnet/efcore/blob/b8483772f298f5ada8b2b5253a9904c93c34919f/src/EFCore/Extensions/EntityFrameworkQueryableExtensions.cs#L2026-L2033
        /// <summary>
        /// The <see cref="System.Reflection.MethodInfo"/> for <see cref="EntityFrameworkQueryableExtensions.Include{TEntity, TProperty}(IQueryable{TEntity}, Expression{Func{TEntity, TProperty}})"/>
        /// </summary>
        internal static readonly MethodInfo IncludeMethodInfo
            = typeof(EntityFrameworkQueryableExtensions)
                .GetTypeInfo().GetDeclaredMethods(nameof(EntityFrameworkQueryableExtensions.Include))
                .Single(
                    mi =>
                        mi.GetGenericArguments().Count() == 2
                        && mi.GetParameters().Any(
                            pi => pi.Name == "navigationPropertyPath" && pi.ParameterType != typeof(string)));
    }
}
