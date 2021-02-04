using AbpQueryFilterDemo.Blogs;
using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace AbpQueryFilterDemo.Posts
{
    public class Post : FullAuditedAggregateRoot<Guid>
    {
        public string Title { get; set; }
        public Guid BlogId { get; set; }
        public Blog Blog { get; set; }

        public Post(Guid id, string title, Guid blogId) : base(id)
        {
            Title = title;
            BlogId = blogId;
        }

        public Post(Guid id, string title, Blog blog) : base(id)
        {
            Title = title;
            Blog = blog;
        }

        protected Post()
        {
            Title = "Post";
        }
    }
}
