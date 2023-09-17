using Be.Cars.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace Be.Cars.Permissions;

public class CarsPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(CarsPermissions.GroupName);
        //Define your own permissions here. Example:
        //myGroup.AddPermission(CarsPermissions.MyPermission1, L("Permission:MyPermission1"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<CarsResource>(name);
    }
}
