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
            var entitiesConfigs = entityTypes.Select(t => 
            {
                var config = _entityConfigurationStore.Get(t);
                var codeProperties = _metadataProvider.GetProperties(t);

                return new {
                    Config = config,
                    Properties = codeProperties,
                    PropertiesMD5 = GetPropertiesMD5(codeProperties),
                };
            }).ToList();

            var dbEntities = await _entityConfigRepository.GetAll().ToListAsync();

            // Update out-of-date configs
            var toUpdate = dbEntities
                .Select(
                    ec =>
                        new { 
                            db = ec, 
                            code = entitiesConfigs.FirstOrDefault(c => c.Config.EntityType.Name == ec.ClassName && c.Config.EntityType.Namespace == ec.Namespace) 
                        })
                .Where(
                    c =>
                        c.code != null &&
                        (c.db.FriendlyName != c.code.Config.FriendlyName || 
                        c.db.TableName != c.code.Config.TableName || 
                        c.db.TypeShortAlias != c.code.Config.SafeTypeShortAlias || 
                        c.db.DiscriminatorValue != c.code.Config.DiscriminatorValue ||
                        c.db.PropertiesMD5 != c.code.PropertiesMD5))
                .ToList();
            foreach (var config in toUpdate)
            {
                config.db.FriendlyName = config.code.Config.FriendlyName;
                config.db.TableName = config.code.Config.TableName;
                config.db.TypeShortAlias = config.code.Config.SafeTypeShortAlias;
                config.db.DiscriminatorValue = config.code.Config.DiscriminatorValue;

                await _entityConfigRepository.UpdateAsync(config.db);

                if (config.db.PropertiesMD5 != config.code.PropertiesMD5)
                    await UpdatePropertiesAsync(config.db, config.code.Config.EntityType, config.code.Properties, config.code.PropertiesMD5);
            }

            // Add news configs
            var toAdd = entitiesConfigs.Where(c => !dbEntities.Any(ec => ec.ClassName == c.Config.EntityType.Name && ec.Namespace == c.Config.EntityType.Namespace)).ToList();
            foreach (var config in toAdd)
            {
                var ec = new EntityConfig()
                {
                    FriendlyName = config.Config.FriendlyName,
                    TableName = config.Config.TableName,
                    TypeShortAlias = config.Config.SafeTypeShortAlias,
                    DiscriminatorValue = config.Config.DiscriminatorValue,
                    ClassName = config.Config.EntityType.Name,
                    Namespace = config.Config.EntityType.Namespace,
                    
                    Source = Domain.Enums.MetadataSourceType.ApplicationCode,
                };
                await _entityConfigRepository.InsertAsync(ec);
                
                await UpdatePropertiesAsync(ec, config.Config.EntityType, config.Properties, config.PropertiesMD5);
            }

            // Inactivate deleted entities
            // todo: write changelog
        }

        private string GetPropertiesMD5(List<PropertyMetadataDto> dtos)
        {
            var propertyProps = typeof(PropertyMetadataDto).GetProperties().OrderBy(p => p.Name).ToList();

            var ordered = dtos.OrderBy(p => p.Path).ToList();

            var sb = new StringBuilder();
            foreach (var dto in ordered) 
            {
                foreach (var prop in propertyProps) 
                {
                    var propValue = prop.GetValue(dto)?.ToString();
                    sb.Append(propValue);
                    sb.Append(";");
                }
                sb.AppendLine();
            }
            return sb.ToString().ToMd5Fingerprint();
        }

        private async Task UpdatePropertiesAsync(EntityConfig entityConfig, Type entityType, List<PropertyMetadataDto> codeProperties, string propertiesMD5)
        {
            try
            {
                // todo: handle inactive flag
                var dbProperties = await _entityPropertyRepository.GetAll().Where(p => p.EntityConfig == entityConfig).ToListAsync();

                var duplicates = codeProperties.GroupBy(p => p.Path, (p, items) => new { Path = p, Items = items }).Where(g => g.Items.Count() > 1).ToList();
                if (duplicates.Any()) 
                { 
                }

                var nextSortOrder = dbProperties.Any()
                    ? dbProperties.Max(p => p.SortOrder) + 1
                    : 0;
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
                            DataFormat = cp.DataFormat,
                            EntityType = cp.EntityTypeShortAlias,
                            ReferenceListName = cp.ReferenceListName,
                            ReferenceListNamespace = cp.ReferenceListNamespace,

                            Source = Domain.Enums.MetadataSourceType.ApplicationCode,
                            SortOrder = nextSortOrder++,
                        };
                        await _entityPropertyRepository.InsertAsync(dbp);
                    }

                    // todo: how to update properties? merge issue
                }

                // todo: inactivate missing properties

                // update properties MD5 to prevent unneeded updates in future
                entityConfig.PropertiesMD5 = propertiesMD5;
                await _entityConfigRepository.InsertAsync(entityConfig);
            }
            catch (Exception e) 
            {
                throw;
            }
        }
    }
}
