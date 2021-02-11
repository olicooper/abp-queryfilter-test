using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;

namespace AbpQueryFilterDemo.Posts
{
    [AllowAnonymous]
    public class PostAppService : AbstractKeyReadOnlyAppService<Post, PostDto, PostListDto, Guid, PostListInput>, IPostAppService
    {
        protected IPostRepository PostRepository => LazyServiceProvider.LazyGetRequiredService<IPostRepository>();
        protected AbpQueryFilterDemo.IDataFilter DataFilter => LazyServiceProvider.LazyGetRequiredService<AbpQueryFilterDemo.IDataFilter>();

        public PostAppService(IRepository<Post> repository) 
            : base(repository) { }

        protected override Task<Post> GetEntityByIdAsync(Guid id)
        {
            return ReadOnlyRepository.FirstOrDefaultAsync(p => p.Id == id);
        }

        public async override Task<PagedResultDto<PostListDto>> GetListAsync(PostListInput input)
        {
            await CheckGetListPolicyAsync();
            
            //NullDisposable.Instance
            using (input.IgnoreSoftDelete ? DataFilter.Disable<ISoftDelete>() : DataFilter.Enable<ISoftDelete>())
            using (input.IgnoreSoftDeleteForBlog ? DataFilter.Disable<ISoftDelete<Blogs.Blog>>() : DataFilter.Enable<ISoftDelete<Blogs.Blog>>())
            //using (input.IgnoreSoftDelete ? DataFilter.Disable<ISoftDelete<Post>>() : DataFilter.Enable<ISoftDelete<Post>>())
            {
                var query = await CreateFilteredQueryAsync(input);

                //if (AbpQueryFilterDemoConsts.UseCustomFiltering && input.IgnoreSoftDelete && input.IgnoreSoftDeleteForBlog)
                //{
                //    query = query.IgnoreAbpQueryFilters();
                //}

                var totalCount = AbpQueryFilterDemoConsts.ExecuteCountQuery ? await AsyncExecuter.CountAsync(query) : 4;

                query = ApplySorting(query, input);
                query = ApplyPaging(query, input);

                var entities = await AsyncExecuter.ToListAsync(query);
                var entityDtos = await MapToGetListOutputDtosAsync(entities);

                return new PagedResultDto<PostListDto>(totalCount, entityDtos);
            }
        }

        protected override async Task<System.Linq.IQueryable<Post>> CreateFilteredQueryAsync(PostListInput input)
        {
            //return (await (input.IncludeDetails 
            //    ? ReadOnlyRepository.WithDetailsAsync(p => p.Blog)
            //    : ReadOnlyRepository.GetQueryableAsync()));

            if (input.UseQuerySyntax)
            {
                return await PostRepository.GetUsingQuerySyntaxAsync(input.IncludeDetails);
            }
            else
            {
                if (input.IncludeDetails)
                {

                    return (await ReadOnlyRepository.GetQueryableAsync())
                        //.IgnoreQueryFilters()

                        // Bypass all ABP filters for a query (ignores filters like ISoftDelete/IMultiTenant)
                        // note: the additional 'Where' calls are to test that the 'IgnoreAbpQueryFilters' call is stripped without stripping any other calls
                        //.Where(p => p.LastModificationTime == null).IgnoreAbpQueryFilters().Where(p => p.LastModificationTime == null)

                        // These could be difficult to evaluate
                        //.Include(p => p.Blog).ThenInclude(b => b.Posts)
                        //.Include(p => p.Blog.Posts)
                        .Include(p => p.Blog)

                        // Thankfully this fails - so we don't need to account for includes with method calls
                        //.Include(p => p.Blog.Posts.First().Blog)
                        ;
                }
                else
                {
                    // This should not filter entities because nothing was included ('Include()' was not called)
                    return await ReadOnlyRepository.GetQueryableAsync();
                }
            }
        }
    }
}
