using AbpQueryFilterDemo.Domain;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;

namespace AbpQueryFilterDemo.Blogs
{
    [AllowAnonymous]
    public class BlogAppService : AbstractKeyReadOnlyAppService<Blog, BlogDto, BlogListDto, Guid, BlogListInput>, IBlogAppService
    {
        protected IRepository<Blog> BlogRepository => LazyServiceProvider.LazyGetRequiredService<IRepository<Blog>>();
        protected IDataFilter DataFilter => LazyServiceProvider.LazyGetRequiredService<IDataFilter>();

        public BlogAppService(IRepository<Blog> repository) 
            : base(repository) { }

        protected override Task<Blog> GetEntityByIdAsync(Guid id)
        {
            return ReadOnlyRepository.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async override Task<PagedResultDto<BlogListDto>> GetListAsync(BlogListInput input)
        {
            await CheckGetListPolicyAsync();

            using (input.IgnoreSoftDelete ? DataFilter.Disable<ISoftDelete>() : DataFilter.Enable<ISoftDelete>())
            {
                var query = await CreateFilteredQueryAsync(input);
                
                //query = query.Where(x => x.Posts.All(x => !x.IsDeleted));

                var totalCount = await AsyncExecuter.CountAsync(query);

                query = ApplySorting(query, input);
                query = ApplyPaging(query, input);

                var entities = await AsyncExecuter.ToListAsync(query);
                var entityDtos = await MapToGetListOutputDtosAsync(entities);

                return new PagedResultDto<BlogListDto>(totalCount, entityDtos);
            }
        }

        protected override async Task<System.Linq.IQueryable<Blog>> CreateFilteredQueryAsync(BlogListInput input)
        {
            return (await (input.IncludeDetails
                ? ReadOnlyRepository.WithDetailsAsync(x => x.Posts)
                : ReadOnlyRepository.GetQueryableAsync()));
        }
    }
}
