using System;
using System.Collections.Generic;
using System.Text;
using AbpQueryFilterDemo.Localization;
using Volo.Abp.Application.Services;

namespace AbpQueryFilterDemo
{
    /* Inherit your application services from this class.
     */
    public abstract class AbpQueryFilterDemoAppService : ApplicationService
    {
        protected AbpQueryFilterDemoAppService()
        {
            LocalizationResource = typeof(AbpQueryFilterDemoResource);
        }
    }
}
