using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp;
using Abp.Authorization;
using Abp.Collections.Extensions;
using Abp.Configuration.Startup;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Localization;
using NHibernate.Linq;
using Shesha.Domain;

namespace Shesha.Authorization
{
    public class ShaPermissionManager : PermissionManager
    {
        private readonly IIocManager _iocManager;
        private readonly IRepository<PermissionDefinition, Guid> _permissionDefinitionRepository;
        private readonly IAuthorizationConfiguration _authorizationConfiguration;

        public ShaPermissionManager(
            IIocManager iocManager,
            IAuthorizationConfiguration authorizationConfiguration, 
            IUnitOfWorkManager unitOfWorkManager,
            IMultiTenancyConfig multiTenancyConfig,
            IRepository<PermissionDefinition, Guid> permissionDefinitionRepository
            ) : 
            base(iocManager, authorizationConfiguration, unitOfWorkManager, multiTenancyConfig)
        {
            _iocManager = iocManager;
            _authorizationConfiguration = authorizationConfiguration;
            _permissionDefinitionRepository = permissionDefinitionRepository;
        }

        /*public override Abp.Authorization.Permission GetPermissionOrNull(string name)
        { 
            var permission = Permissions.GetOrDefault(name);

            if (permission == null)
            {
                var dbPermission = _permissionDefinitionRepository.GetAll().FirstOrDefault(x => x.Name == name);
                if (dbPermission != null)
                {
                    if (!string.IsNullOrEmpty(dbPermission.Parent))
                    {
                        var dbParent = GetPermissionOrNull(dbPermission.Parent);
                        permission = dbParent.CreateChildPermission(dbPermission.Name, L(dbPermission.DisplayName), L(dbPermission.Description));
                    }
                    else
                    {
                        permission = CreatePermission(dbPermission.Name, L(dbPermission.DisplayName), L(dbPermission.Description));
                    }
                }
            }

            return permission;
        }*/

        public override Abp.Authorization.Permission GetPermission(string name)
        {
            //return base.GetPermission(name);

            var permission = GetPermissionOrNull(name);
            if (permission == null)
            {
                throw new AbpException("There is no permission with name: " + name);
            }

            return permission;
        }
    }
}