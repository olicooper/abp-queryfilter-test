using AbpQueryFilterDemo.Domain;
using AbpQueryFilterDemo.Posts;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace AbpQueryFilterDemo.Blogs
{
    [AllowAnonymous]
    public class BlogAppService : AbstractKeyReadOnlyAppService<Blog, BlogDto, BlogListDto, Guid, BlogListInput>, IBlogAppService
    {
        protected IRepository<Post> PostRepository => LazyServiceProvider.LazyGetRequiredService<IRepository<Post>>();

        public BlogAppService(IRepository<Blog> repository) 
            : base(repository) { }

        protected override Task<Blog> GetEntityByIdAsync(Guid id)
        {
            return ReadOnlyRepository.FirstOrDefaultAsync(x => x.Id == id);
        }

        protected override async Task<System.Linq.IQueryable<Blog>> CreateFilteredQueryAsync(BlogListInput input)
        {
            return (await (input.IncludeDetails
                ? ReadOnlyRepository.WithDetailsAsync(x => x.Posts)
                : ReadOnlyRepository.GetQueryableAsync()))
                    .IgnoreAbpQueryFilter(x => x.Posts);
        }
    }
}
