using AbpQueryFilterDemo.Posts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace AbpQueryFilterDemo.EntityFrameworkCore
{
    public class PostRepository : EfCoreRepository<AbpQueryFilterDemoDbContext, Post, Guid>, IPostRepository
    {
        public PostRepository(IDbContextProvider<AbpQueryFilterDemoDbContext> dbContextProvider)
            : base(dbContextProvider)
        {

        }

        /// <summary>
        /// Test 'GetList' method to ensure that filters are correctly applied when using LINQ 'query syntax' instead of the traditional 'method syntax' used by ABP.
        /// <para>
        ///     'Query syntax' is translatable to an Expression tree (see <see cref="IQueryable.Expression"/>), so this query should perform the same as queries written using 'method syntax'.
        /// </para>
        /// </summary>
        public async Task<IQueryable<Post>> GetUsingQuerySyntaxAsync(bool includeDetails = false)
        {
            var context = await GetDbContextAsync();

            if (includeDetails)
            {
                return from post in context.Posts
                       join blog in context.Blogs on post.BlogId equals blog.Id
                       select post;
            }
            else
            {
                return from post in context.Posts
                       select post;
            }
        }
    }
}
