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
        protected IDataFilter DataFilter => LazyServiceProvider.LazyGetRequiredService<IDataFilter>();

        public PostAppService(IRepository<Post> repository) 
            : base(repository) { }

        protected override Task<Post> GetEntityByIdAsync(Guid id)
        {
            return ReadOnlyRepository.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async override Task<PagedResultDto<PostListDto>> GetListAsync(PostListInput input)
        {
            await CheckGetListPolicyAsync();

            using (input.IgnoreSoftDelete ? DataFilter.Disable<ISoftDelete>() : DataFilter.Enable<ISoftDelete>())
            using (input.IgnoreSoftDeleteForBlog ? DataFilter.Disable<ISoftDelete<Blogs.Blog>>() : DataFilter.Enable<ISoftDelete<Blogs.Blog>>())
            {
                var query = await CreateFilteredQueryAsync(input);
                
                //query = query.Where(x => !x.Blog.IsDeleted);

                var totalCount = await AsyncExecuter.CountAsync(query);
                //var totalCount = 4;

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
            //    ? ReadOnlyRepository.WithDetailsAsync(x => x.Blog)
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
                        //.Where(x => x.LastModificationTime == null).IgnoreAbpQueryFilters().Where(x => x.LastModificationTime == null)

                        // This could be difficult to evaluate
                        //.Include(x => x.Blog).ThenInclude(x => x.Posts)
                        //.Include(x => x.Blog.Posts)
                        .Include(x => x.Blog)

                        // Thankfully this fails - so we don't need to account for includes with method calls
                        //.Include(x => x.Blog.Posts.First().Blog)
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
