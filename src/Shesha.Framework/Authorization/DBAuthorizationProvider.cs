using System;
using System.Collections.Generic;
using System.Linq;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Localization;
using Abp.MultiTenancy;
using Shesha.Domain;

namespace Shesha.Authorization
{
    public class DBAuthorizationProvider : AuthorizationProvider
    {

        private readonly IRepository<PermissionDefinition, Guid> _permissionDefinitionRepository;

        public DBAuthorizationProvider(
            IRepository<PermissionDefinition, Guid> permissionDefinitionRepository
            )
        {
            _permissionDefinitionRepository = permissionDefinitionRepository;
        }

        [UnitOfWork]
        public override void SetPermissions(IPermissionDefinitionContext context)
        {
            var dbPermissions = _permissionDefinitionRepository.GetAllList();

            // Update DB-related items
            var dbRootPermissions = dbPermissions.Where(x => string.IsNullOrEmpty(x.Parent)).ToList();
            foreach (var dbPermission in dbRootPermissions)
            {
                var permission = context.CreatePermission(dbPermission.Name, L(dbPermission.DisplayName), L(dbPermission.Description));
                CreateChildPermissions(dbPermissions, permission);
                dbPermissions.Remove(dbPermission);
            }


            // Update code-related items
            var completed = false;
            while (dbPermissions.Any() && !completed)
            {
                var dbPermission = dbPermissions.FirstOrDefault();
                if (dbPermission != null)
                {
                    var permission = context.GetPermissionOrNull(dbPermission.Parent);
                    while (permission == null && dbPermissions.Any(x => x.Name == dbPermission.Parent))
                    {
                        dbPermission = dbPermissions.FirstOrDefault(x => x.Name == dbPermission.Parent);
                        permission = context.GetPermissionOrNull(dbPermission.Parent);
                    }

                    if (permission != null)
                    {
                        CreateChildPermissions(dbPermissions, permission);
                    }
                    else
                    {
                        // remove permission with missed parent
                        _permissionDefinitionRepository.Delete(dbPermission);
                    }
                    dbPermissions.Remove(dbPermission);
                }
            }
        }

        private void CreateChildPermissions(List<PermissionDefinition> dbPermissions, Abp.Authorization.Permission permission)
        {
            var dbChildPermissions = dbPermissions.Where(x => x.Parent == permission.Name).ToList();
            foreach (var dbChildPermission in dbChildPermissions)
            {
                var childPermission =
                    permission.CreateChildPermission(dbChildPermission.Name, L(dbChildPermission.DisplayName), L(dbChildPermission.Description));
                CreateChildPermissions(dbPermissions, childPermission);
                dbPermissions.Remove(dbChildPermission);
            }
        }

        private static ILocalizableString L(string name)
        {
            return new LocalizableString(name, SheshaConsts.LocalizationSourceName);
        }
    }
}
