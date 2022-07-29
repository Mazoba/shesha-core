using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Linq;
using Abp.Reflection;
using Shesha.Bootstrappers;
using Shesha.Domain.ConfigurationItems;
using Shesha.Reflection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Shesha.ConfigurationItems
{
    /// <summary>
    /// Configurable modules bootstrapper
    /// </summary>
    public class ConfigurableModuleBootstrapper : IBootstrapper, ITransientDependency
    {
        private readonly ITypeFinder _typeFinder;
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly IRepository<Module, Guid> _moduleRepo;

        public ConfigurableModuleBootstrapper(ITypeFinder typeFinder, IUnitOfWorkManager unitOfWorkManager, IRepository<Module, Guid> moduleRepo)
        {
            _typeFinder = typeFinder;
            _unitOfWorkManager = unitOfWorkManager;
            _moduleRepo = moduleRepo;
        }

        public async Task Process()
        {
            return;
            using (_unitOfWorkManager.Current.DisableFilter(AbpDataFilters.SoftDelete))
            {
                await DoProcess();
            }
        }

        private async Task DoProcess()
        {
            var codeModules = _typeFinder
                .Find(type => type != null && type.IsPublic && !type.IsGenericType && type.HasAttribute<ConfigurableModuleAttribute>())
                .Select(e => new
                {
                    ModuleType = e,
                    Attribute = e.GetAttribute<ConfigurableModuleAttribute>()
                })
                .ToList();

            foreach (var codeModule in codeModules)
            {
                var dbModule = await _moduleRepo.FirstOrDefaultAsync(codeModule.Attribute.Id);

                // Add module if missing
                if (dbModule == null) 
                {
                    dbModule = new Module
                    {
                        Id = codeModule.Attribute.Id,
                        Name = codeModule.Attribute.Name,
                        Description = codeModule.Attribute.Description,
                    };
                    await _moduleRepo.InsertAsync(dbModule);
                }
            }
        }
    }
}