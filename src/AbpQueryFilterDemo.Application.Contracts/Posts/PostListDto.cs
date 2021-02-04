using System;
using Volo.Abp.Application.Dtos;

namespace AbpQueryFilterDemo.Posts
{
    public class PostListDto : FullAuditedEntityDto<Guid>
    {
        public string Title { get; set; }
        public Blogs.BlogItem Blog { get; set; }

        protected PostListDto()
        {
            Title = "Post";
            //Blog = new();
        }
    }

    public class PostItem : EntityDto<Guid>
    {
        public string Title { get; set; }
        public bool IsDeleted { get; set; }
    }
}
