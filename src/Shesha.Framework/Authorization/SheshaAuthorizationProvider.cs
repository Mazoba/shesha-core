using Abp.Authorization;
using Abp.Localization;
using Abp.MultiTenancy;

namespace Shesha.Authorization
{
    public class SheshaAuthorizationProvider : AuthorizationProvider
    {
        public override void SetPermissions(IPermissionDefinitionContext context)
        {
            context.CreatePermission(PermissionNames.Pages_Users, L("Users"));
            context.CreatePermission(PermissionNames.Pages_Roles, L("Roles"));
            context.CreatePermission(PermissionNames.Pages_Tenants, L("Tenants"), multiTenancySides: MultiTenancySides.Host);

            // Shesha part
            context.CreatePermission(ShaPermissionNames.Application_Configurator, L("ApplicationConfigurator"));
            context.CreatePermission(ShaPermissionNames.Pages_Persons, L("Persons"));
            context.CreatePermission(ShaPermissionNames.Pages_ShaRoles, L("ShaRoles"));
            context.CreatePermission(ShaPermissionNames.Pages_ApplicationSettings, L("ApplicationSettings"));
            context.CreatePermission(ShaPermissionNames.Pages_Maintenance, L("Maintenance"));
        }

        private static ILocalizableString L(string name)
        {
            return new LocalizableString(name, SheshaConsts.LocalizationSourceName);
        }
    }
}
