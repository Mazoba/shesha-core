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
using Abp.Domain.Uow;
using Abp.ObjectMapping;
using Shesha.Permissions;

namespace Shesha.Permission
{
    public class PermissionedObjectsBootstrapper : IBootstrapper, ITransientDependency
    {
        private readonly IRepository<PermissionedObject, Guid> _permissionedObjectRepository;
        private readonly IObjectMapper _objectMapper;

        public PermissionedObjectsBootstrapper(IRepository<PermissionedObject, Guid> permissionedObjectRepository, IObjectMapper objectMapper)
        {
            _permissionedObjectRepository = permissionedObjectRepository;
            _objectMapper = objectMapper;
        }

        public async Task Process()
        {
            var providers = IocManager.Instance.ResolveAll<IPermissionedObjectProvider>();
            foreach (var permissionedObjectProvider in providers)
            {
                var items  = permissionedObjectProvider.GetAll();
                var category = permissionedObjectProvider.GetCategory();

                var dbItems = await _permissionedObjectRepository.GetAll().Where(x => x.Category == category).ToListAsync();

                // ToDo: think how to update Protected objects in th bootstrapper

                // Add news items
                var toAdd = items.Where(i => !dbItems.Any(dbi => dbi.Object == i.Object && dbi.Category == i.Category))
                    .ToList();
                foreach (var item in toAdd)
                {
                    await _permissionedObjectRepository.InsertAsync(_objectMapper.Map<PermissionedObject>(item));
                }

                // Inactivate deleted items
                var toDelete = dbItems
                    .Where(dbi => !items.Any(i => dbi.Object == i.Object && dbi.Category == i.Category)).ToList();
                foreach (var item in toDelete)
                {
                    await _permissionedObjectRepository.DeleteAsync(item);
                }
            }

            // todo: write changelog
        }
    }
}
