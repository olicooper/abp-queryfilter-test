using AbpQueryFilterDemo.Blogs;
using System.Threading.Tasks;

namespace AbpQueryFilterDemo.Web.Pages
{
    public class IndexModel : AbpQueryFilterDemoPageModel
    {
        protected IBlogAppService BlogAppService => LazyServiceProvider.LazyGetRequiredService<IBlogAppService>();

        public void OnGet()
        {

        }

        //public async Task OnGetAsync()
        //{
        //    //var items = await BlogAppService.GetListAsync(new BlogListInput { });

        //    //ViewData["items"] = items;
        //}
    }
}