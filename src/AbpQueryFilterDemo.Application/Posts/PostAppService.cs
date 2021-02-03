using AbpQueryFilterDemo.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace AbpQueryFilterDemo.Posts
{
    [AllowAnonymous]
    public class PostAppService : AbstractKeyReadOnlyAppService<Post, PostDto, PostListDto, Guid, PostListInput>, IPostAppService
    {
        protected IRepository<Post> PostRepository => LazyServiceProvider.LazyGetRequiredService<IRepository<Post>>();

        public PostAppService(IRepository<Post> repository) 
            : base(repository) { }

        protected override Task<Post> GetEntityByIdAsync(Guid id)
        {
            return ReadOnlyRepository.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async override Task<PagedResultDto<PostListDto>> GetListAsync(PostListInput input)
        {
            await CheckGetListPolicyAsync();

            var query = await CreateFilteredQueryAsync(input);

            var totalCount = await AsyncExecuter.CountAsync(query);
            //var totalCount = 4;

            query = ApplySorting(query, input);
            query = ApplyPaging(query, input);

            var entities = await AsyncExecuter.ToListAsync(query);
            var entityDtos = await MapToGetListOutputDtosAsync(entities);

            return new PagedResultDto<PostListDto>(totalCount, entityDtos);
        }

        protected override async Task<System.Linq.IQueryable<Post>> CreateFilteredQueryAsync(PostListInput input)
        {
            //return (await (input.IncludeDetails 
            //    ? ReadOnlyRepository.WithDetailsAsync(x => x.Blog)
            //    : ReadOnlyRepository.GetQueryableAsync()))
            //        .IgnoreAbpQueryFilter(x => x.Blog);
            
            if (input.IncludeDetails)
            {
                return (await ReadOnlyRepository.GetQueryableAsync())
                    .IgnoreAbpQueryFilter(x => x.Blog)
                    //.IgnoreAbpQueryFilter(x => x.Blog.Posts)

                    //.IgnoreQueryFilters()

                    // This could be difficult to evaluate
                    .Include(x => x.Blog).ThenInclude(x => x.Posts)
                    //.Include(x => x.Blog.Posts)
                    //.Include(x => x.Blog)

                    // Thankfully this fails - so we don't need to account for includes with method calls
                    //.Include(x => x.Blog.Posts.First().Blog)
                    ;
            }
            else
            {
                return (await ReadOnlyRepository.GetQueryableAsync())
                    // This should have no effect because nothing was included ('Include()' was not called)
                    .IgnoreAbpQueryFilter(x => x.Blog);
            }
        }
    }
}
