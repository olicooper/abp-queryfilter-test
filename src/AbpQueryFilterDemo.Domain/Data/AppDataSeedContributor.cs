using AbpQueryFilterDemo.Blogs;
using AbpQueryFilterDemo.Posts;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;

namespace AbpQueryFilterDemo.IdentityServer
{
    public class AppDataSeedContributor : IDataSeedContributor, ITransientDependency
    {
        private readonly IRepository<Blog> _blogRepository;
        private readonly IRepository<Post> _postRepository;

        private readonly IDataFilter _dataFilter;
        private readonly IGuidGenerator _guidGenerator;
        private readonly IConfiguration _configuration;
        private readonly ICurrentTenant _currentTenant;

        public AppDataSeedContributor(
            IRepository<Blog> blogRepository,
            IRepository<Post> postRepository,
            IDataFilter dataFilter,
            IGuidGenerator guidGenerator,
            IConfiguration configuration,
            ICurrentTenant currentTenant)
        {
            _postRepository = postRepository;
            _blogRepository = blogRepository;

            _dataFilter = dataFilter;
            _guidGenerator = guidGenerator;
            _configuration = configuration;
            _currentTenant = currentTenant;
        }

        [UnitOfWork]
        public virtual async Task SeedAsync(DataSeedContext context)
        {
            using (_currentTenant.Change(context?.TenantId))
            {
                await CreateBlogs();
            }
        }

        private async Task CreateBlogs()
        {
            long existingBlogCount = 0;
            
            using (_dataFilter.Disable<ISoftDelete>())
            {
                existingBlogCount = await _blogRepository.GetCountAsync();
            }

            //List<Guid>
            //    blogIds = new()
            //    {
            //        new Guid("C8A39C37-8F2A-4D9B-AC18-959C8F9B95FC"),
            //        new Guid("E5FB2114-4FE6-46F1-A415-4B1312B50A31"),
            //    },
            //    postIds = new()
            //    {
            //        new Guid("794BA2C5-8A6B-478D-808B-3D5CA1D722B6"),
            //        new Guid("7B81423B-AF5F-4363-A82C-538C1FF1650D"),
            //        new Guid("468350CB-FB03-4891-8A2A-CC0A2EF0D5A9"),
            //        new Guid("5D1EE3A6-4A53-4DA6-940B-C62040C7D89F"),
            //    };

            //if (existingBlogCount >= blogIds.Count) return;

            //var blogsToInsert = new List<Blog>();

            //for (int i = 0; i < blogIds.Count; i++)
            //{
            //    if (existingBlogs.Any(x => x.Id == blogIds[i])) continue;

            //    blogsToInsert.Add(new(blogIds[i], $"Blog {i + 1}"));

            //    blogsToInsert[^1].Posts = new List<Post>
            //    {
            //        new(postIds[i * 2], $"{blogsToInsert[^1].Name} - Post {i * 2 + 1}", blogsToInsert[^1]),
            //        new(postIds[i * 2 + 1], $"{blogsToInsert[^1].Name} - Post {i * 2 + 2}", blogsToInsert[^1]),
            //    };
            //}

            //await _blogRepository.InsertManyAsync(blogsToInsert);


            List<Blog> blogsToCreate = new()
            {
                new(new Guid("C8A39C37-8F2A-4D9B-AC18-959C8F9B95FC"), "Blog 1"),
                new(new Guid("E5FB2114-4FE6-46F1-A415-4B1312B50A31"), "[DELETED] Blog 2")
                {
                    IsDeleted = true,
                    DeletionTime = DateTime.Now
                }
            };

            if (existingBlogCount >= blogsToCreate.Count) return;

            // Blog 1 posts
            blogsToCreate[0].Posts.Add(
                new(new Guid("794BA2C5-8A6B-478D-808B-3D5CA1D722B6"), $"[DELETED] Post 1 ({blogsToCreate[0].Name})", blogsToCreate[0])
                {
                    IsDeleted = true,
                    DeletionTime = DateTime.Now
                }
            );
            blogsToCreate[0].Posts.Add(
                new(new Guid("7B81423B-AF5F-4363-A82C-538C1FF1650D"), $"Post 2 ({blogsToCreate[0].Name})", blogsToCreate[0])
            );

            // Blog 2 posts
            blogsToCreate[1].Posts.Add(
                new(new Guid("468350CB-FB03-4891-8A2A-CC0A2EF0D5A9"), $"Post 3 ({blogsToCreate[1].Name})", blogsToCreate[1])
            );
            blogsToCreate[1].Posts.Add(
                new(new Guid("5D1EE3A6-4A53-4DA6-940B-C62040C7D89F"), $"Post 4 ({blogsToCreate[1].Name})", blogsToCreate[1])
            );

            await _blogRepository.InsertManyAsync(blogsToCreate);

        }
    }
}
