using Volo.Abp.Settings;

namespace AbpQueryFilterDemo.Settings
{
    public class AbpQueryFilterDemoSettingDefinitionProvider : SettingDefinitionProvider
    {
        public override void Define(ISettingDefinitionContext context)
        {
            //Define your own settings here. Example:
            //context.Add(new SettingDefinition(AbpQueryFilterDemoSettings.MySetting1));
        }
    }
}
