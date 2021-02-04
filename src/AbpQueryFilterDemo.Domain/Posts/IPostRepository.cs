using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace AbpQueryFilterDemo.Posts
{
    public interface IPostRepository : IRepository<Post, Guid>
    {
        Task<IQueryable<Post>> GetUsingQuerySyntaxAsync(bool includeDetails = false);
    }
}
