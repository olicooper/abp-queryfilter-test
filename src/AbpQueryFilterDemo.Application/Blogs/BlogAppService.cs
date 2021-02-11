using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;

namespace AbpQueryFilterDemo.Blogs
{
    [AllowAnonymous]
    public class BlogAppService : AbstractKeyReadOnlyAppService<Blog, BlogDto, BlogListDto, Guid, BlogListInput>, IBlogAppService
    {
        protected IRepository<Blog> BlogRepository => LazyServiceProvider.LazyGetRequiredService<IRepository<Blog>>();
        protected Posts.IPostRepository PostRepository => LazyServiceProvider.LazyGetRequiredService<Posts.IPostRepository>();
        protected AbpQueryFilterDemo.IDataFilter DataFilter => LazyServiceProvider.LazyGetRequiredService<AbpQueryFilterDemo.IDataFilter>();

        public BlogAppService(IRepository<Blog> repository) 
            : base(repository) { }

        protected override Task<Blog> GetEntityByIdAsync(Guid id)
        {
            return ReadOnlyRepository.FirstOrDefaultAsync(b => b.Id == id);
        }

        public async override Task<PagedResultDto<BlogListDto>> GetListAsync(BlogListInput input)
        {
            await CheckGetListPolicyAsync();

            using (input.IgnoreSoftDelete ? DataFilter.Disable<ISoftDelete>() : DataFilter.Enable<ISoftDelete>())
            using (input.IgnoreSoftDeleteForPosts ? DataFilter.Disable<ISoftDelete<Posts.Post>>() : DataFilter.Enable<ISoftDelete<Posts.Post>>())
            //using (input.IgnoreSoftDelete ? DataFilter.Disable<ISoftDelete<Blog>>() : DataFilter.Enable<ISoftDelete<Blog>>())
            {
                var query = await CreateFilteredQueryAsync(input);
                //if (AbpQueryFilterDemoConsts.UseCustomFiltering && input.IgnoreSoftDelete && input.IgnoreSoftDeleteForPosts)
                //{
                //    query = query.IgnoreAbpQueryFilters();
                //}

                var totalCount = AbpQueryFilterDemoConsts.ExecuteCountQuery ? await AsyncExecuter.CountAsync(query) : 2;

                query = ApplySorting(query, input);
                query = ApplyPaging(query, input);

                var entities = await AsyncExecuter.ToListAsync(query);
                var entityDtos = await MapToGetListOutputDtosAsync(entities);

                return new PagedResultDto<BlogListDto>(totalCount, entityDtos);
            }
        }

        protected override async Task<System.Linq.IQueryable<Blog>> CreateFilteredQueryAsync(BlogListInput input)
        {
            if (input.IncludeDetails)
            {
                if (input.UseIncludeFilter)
                {
                    // https://docs.microsoft.com/en-us/ef/core/querying/related-data/eager#filtered-include
                    return (await ReadOnlyRepository.GetQueryableAsync())
                        .Include(x => x.Posts
                            // Note: Ensures the collection is not loaded in to memory prior to evaluation (not really required here though)
                            .AsQueryable()
                            .Where(x => x.ConcurrencyStamp != null && x.ExtraProperties != null)
                        )

                        // see: https://docs.microsoft.com/en-us/ef/core/querying/single-split-queries#split-queries-1

                        // Note: Default behaviour for collections. Creates a filtered SQL query for root 'Blog' entity, then another Query for filtered Blog.Posts when they are projected/accessed.
                        //.AsSplitQuery()

                        // Note: Default behaviour for on-to-one entities. Forces all data to be fetched in one query (and generates a 'LEFT JOIN' on Posts table). Can cause cartesian explosion when working with collections.
                        //.AsSingleQuery()
                        ;
                }

                return await ReadOnlyRepository.WithDetailsAsync(b => b.Posts);
            }

            return await ReadOnlyRepository.GetQueryableAsync();
        }
    }
}
