using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Reflection;
using NHibernate.Linq;
using Shesha.Bootstrappers;
using Shesha.Configuration.Runtime;
using Shesha.Domain;
using Shesha.Metadata;
using Shesha.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Shesha.DynamicEntities
{
    public class EntityConfigsBootstrapper : IBootstrapper, ITransientDependency
    {
        private readonly IRepository<EntityConfig, Guid> _entityConfigRepository;
        private readonly IRepository<EntityProperty, Guid> _entityPropertyRepository;
        // todo: remove usage of IEntityConfigurationStore
        private readonly IEntityConfigurationStore _entityConfigurationStore;
        private readonly IAssemblyFinder _assembleFinder;
        private readonly IMetadataProvider _metadataProvider;

        public EntityConfigsBootstrapper(IRepository<EntityConfig, Guid> entityConfigRepository, IEntityConfigurationStore entityConfigurationStore, IAssemblyFinder assembleFinder, IRepository<EntityProperty, Guid> entityPropertyRepository, IMetadataProvider metadataProvider)
        {
            _entityConfigRepository = entityConfigRepository;
            _entityConfigurationStore = entityConfigurationStore;
            _assembleFinder = assembleFinder;
            _entityPropertyRepository = entityPropertyRepository;
            _metadataProvider = metadataProvider;
        }

        public async Task Process()
        {
            var assemblies = _assembleFinder.GetAllAssemblies()
                    .Distinct(new AssemblyFullNameComparer())
                    .Where(a => !a.IsDynamic &&
                                a.GetTypes().Any(t => MappingHelper.IsEntity(t))
                    )
                    .ToList();

            foreach (var assembly in assemblies) 
            {
                await ProcessAssemblyAsync(assembly);
            }
        }

        private async Task ProcessAssemblyAsync(Assembly assembly) 
        {
            var entityTypes = assembly.GetTypes().Where(t => MappingHelper.IsEntity(t)).ToList();
            // todo: remove usage of IEntityConfigurationStore
            var entitiesConfigs = entityTypes.Select(t => _entityConfigurationStore.Get(t)).ToList();

            var dbEntities = await _entityConfigRepository.GetAll().ToListAsync();

            // Update out-of-date configs
            var toUpdate = dbEntities
                .Select(
                    ec =>
                        new { db = ec, code = entitiesConfigs.FirstOrDefault(c => c.EntityType.Name == ec.ClassName && c.EntityType.Namespace == ec.Namespace) })
                .Where(
                    c =>
                        c.code != null &&
                        (c.db.FriendlyName != c.code.FriendlyName || c.db.TableName != c.code.TableName || c.db.TypeShortAlias != c.code.SafeTypeShortAlias || c.db.DiscriminatorValue != c.code.DiscriminatorValue))
                .ToList();
            foreach (var config in toUpdate)
            {
                config.db.FriendlyName = config.code.FriendlyName;
                config.db.TableName = config.code.TableName;
                config.db.TypeShortAlias = config.code.SafeTypeShortAlias;
                config.db.DiscriminatorValue = config.code.DiscriminatorValue;

                await _entityConfigRepository.UpdateAsync(config.db);

                await UpdatePropertiesAsync(config.db, config.code.EntityType);
            }

            // Add news configs
            var toAdd = entitiesConfigs.Where(c => !dbEntities.Any(ec => ec.ClassName == c.EntityType.Name && ec.Namespace == c.EntityType.Namespace)).ToList();
            foreach (var config in toAdd)
            {
                var ec = new EntityConfig()
                {
                    FriendlyName = config.FriendlyName,
                    TableName = config.TableName,
                    TypeShortAlias = config.SafeTypeShortAlias,
                    DiscriminatorValue = config.DiscriminatorValue,
                    ClassName = config.EntityType.Name,
                    Namespace = config.EntityType.Namespace,
                    
                    Source = Domain.Enums.MetadataSourceType.ApplicationCode,
                };
                await _entityConfigRepository.InsertAsync(ec);
                
                await UpdatePropertiesAsync(ec, config.EntityType);
            }

            // Inactivate deleted entities
            // todo: write changelog
        }

        private async Task UpdatePropertiesAsync(EntityConfig entityConfig, Type entityType)
        {
            try
            {
                // todo: handle inactive flag

                var dbProperties = await _entityPropertyRepository.GetAll().Where(p => p.EntityConfig == entityConfig).ToListAsync();
                var codeProperties = _metadataProvider.GetProperties(entityType);

                var duplicates = codeProperties.GroupBy(p => p.Path, (p, items) => new { Path = p, Items = items }).Where(g => g.Items.Count() > 1).ToList();
                if (duplicates.Any()) 
                { 
                }

                foreach (var cp in codeProperties)
                {
                    var dbp = dbProperties.FirstOrDefault(p => p.Name == cp.Path);
                    if (dbp == null)
                    {
                        dbp = new EntityProperty
                        {
                            EntityConfig = entityConfig,
                            Name = cp.Path,
                            Label = cp.Label,
                            Description = cp.Description,
                            DataType = cp.DataType,
                            EntityType = cp.EntityTypeShortAlias,
                            ReferenceListName = cp.ReferenceListName,
                            ReferenceListNamespace = cp.ReferenceListNamespace,

                            Source = Domain.Enums.MetadataSourceType.ApplicationCode,
                        };
                        await _entityPropertyRepository.InsertAsync(dbp);
                    }

                    // todo: how to update properties? merge issue
                }

                // todo: inactivate missing properties
            }
            catch (Exception e) 
            {
                throw;
            }
        }
    }
}
