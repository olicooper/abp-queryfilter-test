using AbpQueryFilterDemo.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace AbpQueryFilterDemo.Permissions
{
    public class AbpQueryFilterDemoPermissionDefinitionProvider : PermissionDefinitionProvider
    {
        public override void Define(IPermissionDefinitionContext context)
        {
            var myGroup = context.AddGroup(AbpQueryFilterDemoPermissions.GroupName);

            //Define your own permissions here. Example:
            //myGroup.AddPermission(AbpQueryFilterDemoPermissions.MyPermission1, L("Permission:MyPermission1"));
        }

        private static LocalizableString L(string name)
        {
            return LocalizableString.Create<AbpQueryFilterDemoResource>(name);
        }
    }
}
