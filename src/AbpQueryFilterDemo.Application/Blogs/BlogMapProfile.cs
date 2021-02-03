using AutoMapper;
using System.Linq;

namespace AbpQueryFilterDemo.Blogs
{
    public class BlogMapProfile : Profile
    {
        public BlogMapProfile()
        {
            CreateMap<Blog, BlogListDto>();
                //.ForMember(x => x.Posts, opt => opt.MapFrom(
                //    x => x.Posts.Select(p => new { p.Id, p.Title })
                //));

            CreateMap<Blog, BlogDto>();
            CreateMap<Blog, BlogItem>();
        }
    }
}
