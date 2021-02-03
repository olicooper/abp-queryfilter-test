using AbpQueryFilterDemo.Posts;
using System;
using System.Collections.Generic;
using Volo.Abp.Domain.Entities.Auditing;

namespace AbpQueryFilterDemo.Blogs
{
    public class Blog : FullAuditedAggregateRoot<Guid>
    {
        public string Name { get; set; }
        public ICollection<Post> Posts { get; set; }

        public Blog(Guid id, string name) : base(id)
        {
            Name = name;
            Posts = new List<Post>();
        }

        protected Blog()
        {
            Name = "Blog";
            Posts = new List<Post>();
        }
    }
}
