using System;
using Volo.Abp.Application.Services;

namespace AbpQueryFilterDemo.Posts
{
    public interface IPostAppService 
        : IReadOnlyAppService<PostDto, PostListDto, Guid, PostListInput>
    { }
}
