using Abp.Dependency;
using Abp.Reflection;
using Abp.Runtime.Caching;
using Shesha.Configuration.Runtime;
using Shesha.Extensions;
using Shesha.Metadata.Dtos;
using Shesha.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Shesha.Metadata
{
    public class EntityModelProvider : BaseModelProvider, ITransientDependency
    {
        private readonly ITypeFinder _typeFinder;
        private readonly IEntityConfigurationStore _entityConfigurationStore;

        public EntityModelProvider(ICacheManager cacheManager, IEntityConfigurationStore entityConfigurationStore, ITypeFinder typeFinder) : base(cacheManager)
        {
            _typeFinder = typeFinder;
            _entityConfigurationStore = entityConfigurationStore;
        }

        protected override Task<List<ModelDto>> FetchModelsAsync()
        {
            var types = _typeFinder.FindAll().Where(t => t.IsEntityType())
                .Select(t => 
                {
                    var config = _entityConfigurationStore.Get(t);
                    return new ModelDto
                    {
                        ClassName = t.FullName,
                        Type = t,
                        Description = ReflectionHelper.GetDescription(t),
                        Alias = config?.SafeTypeShortAlias
                    };
                })
                .ToList();

            return Task.FromResult(types);
        }
    }
}
