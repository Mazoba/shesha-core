using System.Collections.Generic;
using Abp.Authorization;
using Abp.Configuration.Startup;
using Abp.Dependency;
using Abp.Domain.Uow;
using Abp.Localization;

namespace Shesha.Authorization
{
    public class ShaPermissionManager : PermissionManager
    {
        public ShaPermissionManager(
            IIocManager iocManager,
            IAuthorizationConfiguration authorizationConfiguration, 
            IUnitOfWorkManager unitOfWorkManager,
            IMultiTenancyConfig multiTenancyConfig
            ) : 
            base(iocManager, authorizationConfiguration, unitOfWorkManager, multiTenancyConfig)
        {
        }

        public override IReadOnlyList<Permission> GetAllPermissions(bool tenancyFilter = true)
        {
            return base.GetAllPermissions(tenancyFilter);
        }

        public override Permission GetPermissionOrNull(string name)
        {
            return base.GetPermissionOrNull(name);
        }

        public override Permission GetPermission(string name)
        {
            try
            {
                return base.GetPermission(name);
            }
            catch
            {
                return new Permission(name, new FixedLocalizableString(name));
            }
        }
    }
}