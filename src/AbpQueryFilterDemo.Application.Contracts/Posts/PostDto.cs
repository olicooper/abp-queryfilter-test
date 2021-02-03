using AbpQueryFilterDemo.Blogs;
using System;
using Volo.Abp.Application.Dtos;

namespace AbpQueryFilterDemo.Posts
{
    public class PostDto : FullAuditedEntityDto<Guid>
    {
        public string Title { get; set; }
        public BlogDto Blog { get; set; }

        protected PostDto()
        {
            Title = "Post";
        }
    }
}
