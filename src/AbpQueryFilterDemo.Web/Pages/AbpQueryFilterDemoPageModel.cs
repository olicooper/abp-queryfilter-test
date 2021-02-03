using AbpQueryFilterDemo.Localization;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace AbpQueryFilterDemo.Web.Pages
{
    /* Inherit your PageModel classes from this class.
     */
    public abstract class AbpQueryFilterDemoPageModel : AbpPageModel
    {
        protected AbpQueryFilterDemoPageModel()
        {
            LocalizationResourceType = typeof(AbpQueryFilterDemoResource);
        }
    }
}