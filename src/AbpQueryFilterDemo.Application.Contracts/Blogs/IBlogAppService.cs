using System;
using Volo.Abp.Application.Services;

namespace AbpQueryFilterDemo.Blogs
{
    public interface IBlogAppService 
        : IReadOnlyAppService<BlogDto, BlogListDto, Guid, BlogListInput>
    { }
}
