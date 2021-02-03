using AutoMapper;
using System;
using System.Collections.Generic;

namespace AbpQueryFilterDemo.Posts
{
    public class PostMapProfile : Profile
    {
        public PostMapProfile()
        {
            CreateMap<Post, PostListDto>();
                //.ForMember(x => x.Blog, opt =>
                //{
                //    opt.Condition(x => x.Blog != null);
                //    opt.MapFrom(x => new { x.Blog.Id, x.Blog.Name });
                //});

            CreateMap<Post, PostDto>();

            CreateMap<Post, PostItem>();
        }
    }
}
