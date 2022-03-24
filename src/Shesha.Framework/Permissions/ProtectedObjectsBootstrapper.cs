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
    public class ProtectedObjectsBootstrapper : IBootstrapper, ITransientDependency
    {
        private readonly IRepository<ProtectedObject, Guid> _protectedObjectRepository;
        private readonly IObjectMapper _objectMapper;

        public ProtectedObjectsBootstrapper(IRepository<ProtectedObject, Guid> protectedObjectRepository, IObjectMapper objectMapper)
        {
            _protectedObjectRepository = protectedObjectRepository;
            _objectMapper = objectMapper;
        }

        public async Task Process()
        {
            var providers = IocManager.Instance.ResolveAll<IProtectedObjectProvider>();
            foreach (var protectedObjectProvider in providers)
            {
                var items  = protectedObjectProvider.GetAll();
                var category = protectedObjectProvider.GetCategory();

                var dbItems = await _protectedObjectRepository.GetAll().Where(x => x.Category == category).ToListAsync();

                // ToDo: think how to update Protected objects in th bootstrapper

                // Add news items
                var toAdd = items.Where(i => !dbItems.Any(dbi => dbi.Object == i.Object && dbi.Category == i.Category))
                    .ToList();
                foreach (var item in toAdd)
                {
                    await _protectedObjectRepository.InsertAsync(_objectMapper.Map<ProtectedObject>(item));
                }

                // Inactivate deleted items
                var toDelete = dbItems
                    .Where(dbi => !items.Any(i => dbi.Object == i.Object && dbi.Category == i.Category)).ToList();
                foreach (var item in toDelete)
                {
                    await _protectedObjectRepository.DeleteAsync(item);
                }
            }

            // todo: write changelog
        }
    }
}
