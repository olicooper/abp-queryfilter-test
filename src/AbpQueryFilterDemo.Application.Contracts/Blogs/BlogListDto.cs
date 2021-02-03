using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace AbpQueryFilterDemo.Blogs
{
    public class BlogListDto : FullAuditedEntityDto<Guid>
    {
        public string Name { get; set; }
        public List<Posts.PostItem> Posts { get; set; }

        protected BlogListDto()
        {
            Name = "Blog";
            Posts = new();
        }
    }

    public class BlogItem : EntityDto<Guid>
    {
        public string Name { get; set; }
    }

}
