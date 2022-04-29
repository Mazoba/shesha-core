using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Reflection;
using NHibernate.Linq;
using Shesha.Bootstrappers;
using Shesha.Configuration.Runtime;
using Shesha.Domain;
using Shesha.Metadata;
using Shesha.Metadata.Dtos;
using Shesha.Reflection;
using Shesha.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Domain.Uow;
using Abp.ObjectMapping;
using Shesha.Permissions;

namespace Shesha.Permission
{
    public class PermissionDefinitionsBootstrapper : IBootstrapper, ITransientDependency
    {
        private readonly IPermissionManager _permissionManager;
        private readonly IRepository<PermissionDefinition, Guid> _permissionDefinitionRepository;

        public PermissionDefinitionsBootstrapper(IPermissionManager permissionManager, IRepository<PermissionDefinition, Guid> permissionDefinitionRepository)
        {
            _permissionManager = permissionManager;
            _permissionDefinitionRepository = permissionDefinitionRepository;
        }

        public async Task Process()
        {
            SetPermissions(_permissionManager as IPermissionDefinitionContext);
            // todo: write changelog
        }

        [UnitOfWork]
        public void SetPermissions(IPermissionDefinitionContext context)
        {
            var dbPermissions = _permissionDefinitionRepository.GetAllList();

            // Update DB-related items
            var dbRootPermissions = dbPermissions.Where(x => string.IsNullOrEmpty(x.Parent)).ToList();
            foreach (var dbPermission in dbRootPermissions)
            {
                var permission = context.CreatePermission(dbPermission.Name, dbPermission.DisplayName.L(), dbPermission.Description.L());
                CreateChildPermissions(dbPermissions, permission);
                dbPermissions.Remove(dbPermission);
            }


            // Update code-related items
            while (dbPermissions.Any())
            {
                var dbPermission = dbPermissions.FirstOrDefault();
                if (dbPermission != null)
                {
                    var permission = context.GetPermissionOrNull(dbPermission.Parent);
                    while (permission == null && dbPermissions.Any(x => x.Name == dbPermission?.Parent))
                    {
                        dbPermission = dbPermissions.FirstOrDefault(x => x.Name == dbPermission?.Parent);
                        permission = context.GetPermissionOrNull(dbPermission?.Parent);
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
                    permission.CreateChildPermission(dbChildPermission.Name, dbChildPermission.DisplayName.L(), dbChildPermission.Description.L());
                CreateChildPermissions(dbPermissions, childPermission);
                dbPermissions.Remove(dbChildPermission);
            }
        }

    }
}
