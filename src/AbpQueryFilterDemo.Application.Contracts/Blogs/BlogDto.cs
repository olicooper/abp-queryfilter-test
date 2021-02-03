using AbpQueryFilterDemo.Posts;
using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace AbpQueryFilterDemo.Blogs
{
    public class BlogDto : FullAuditedEntityDto<Guid>
    {
        public string Name { get; set; }
        public ICollection<PostDto> Posts { get; set; }

        protected BlogDto()
        {
            Name = "Blog";
            Posts = new List<PostDto>();
        }
    }
}
