using AbpQueryFilterDemo.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace AbpQueryFilterDemo.Controllers
{
    /* Inherit your controllers from this class.
     */
    public abstract class AbpQueryFilterDemoController : AbpController
    {
        protected AbpQueryFilterDemoController()
        {
            LocalizationResource = typeof(AbpQueryFilterDemoResource);
        }
    }
}