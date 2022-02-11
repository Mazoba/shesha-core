using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Reflection;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using NHibernate.Linq;
using Shesha.Configuration.Runtime;
using Shesha.Domain;
using Shesha.Domain.Enums;
using Shesha.DynamicEntities.Dtos;
using Shesha.Services.VersionedFields;

namespace Shesha.DynamicEntities
{
    /// inheritedDoc
    public class DynamicPropertyManager : IDynamicPropertyManager, ITransientDependency
    {
        private readonly IRepository<EntityProperty, Guid> _entityPropertyRepository;
        private readonly IRepository<EntityPropertyValue, Guid> _entityPropertyValueRepository;
        
        public IDynamicDtoTypeBuilder DtoTypeBuilder { get; set; }
        public ISerializationManager SerializationManager { get; set; }

        public DynamicPropertyManager(
            IRepository<EntityProperty, Guid> entityPropertyRepository,
            IRepository<EntityPropertyValue, Guid> entityPropertyValueRepository
            )
        {
            _entityPropertyRepository = entityPropertyRepository;
            _entityPropertyValueRepository = entityPropertyValueRepository;
        }

        public async Task<string> GetValueAsync<TId>(IEntity<TId> entity, EntityPropertyDto property)
        {
            var config = entity.GetType().GetEntityConfiguration();

            var result = await _entityPropertyValueRepository.GetAll()
                .Where(x => x.EntityProperty.Id == property.Id && x.OwnerId == entity.Id.ToString() && x.OwnerType == config.TypeShortAlias)
                .OrderByDescending(x => x.CreationTime).FirstOrDefaultAsync();
                
            return result?.Value;
        }

        // todo: get IsVersioned flag from the EntityPropertyDto
        public async Task SetValueAsync<TId>(IEntity<TId> entity, EntityPropertyDto property, string value, bool createNewVersion)
        {
            var config = entity.GetType().GetEntityConfiguration();

            var prop = _entityPropertyValueRepository.GetAll()
                .Where(x => x.EntityProperty.Id == property.Id && x.OwnerId == entity.Id.ToString() &&
                            x.OwnerType == config.TypeShortAlias)
                .OrderByDescending(x => x.CreationTime).FirstOrDefault();

            if (prop?.Value == value) return;

            if (createNewVersion || prop == null)
            {
                prop = new EntityPropertyValue() { Value = value, EntityProperty = _entityPropertyRepository.Get(property.Id) };
                prop.SetOwner(entity);
            }
            else
            {
                prop.Value = value;
            }

            await _entityPropertyValueRepository.InsertOrUpdateAsync(prop);
        }

        public async Task MapDtoToEntityAsync<TId, TDynamicDto, TEntity>(TDynamicDto dynamicDto, TEntity entity)
            where TEntity : class, IEntity<TId>
            where TDynamicDto : class, IDynamicDto<TEntity, TId>
        {
            await MapPropertiesAsync(entity, dynamicDto, async (ent, dto, entProp, dtoProp) =>
            {
                var rawValue = dtoProp.GetValue(dto);
                var convertedValue = SerializationManager.SerializeProperty(entProp, rawValue);
                await SetValueAsync(entity, entProp, convertedValue, false);
            });
        }

        public async Task MapEntityToDtoAsync<TId, TDynamicDto, TEntity>(TEntity entity, TDynamicDto dynamicDto )
            where TEntity : class, IEntity<TId>
            where TDynamicDto : class, IDynamicDto<TEntity, TId>
        {
            await MapPropertiesAsync(entity, dynamicDto, async (ent, dto, entProp, dtoProp) =>
            {
                var serializedValue = await GetValueAsync(entity, entProp);
                var rawValue = serializedValue != null
                    ? SerializationManager.DeserializeProperty(dtoProp.PropertyType, serializedValue)
                    : null;
                dtoProp.SetValue(dto, rawValue);
            });
        }

        public async Task MapPropertiesAsync<TId, TDynamicDto>(IEntity<TId> entity, TDynamicDto dto, 
            Func<IEntity<TId>, TDynamicDto, EntityPropertyDto, PropertyInfo, Task> action)
        {
            var dynamicProperties = (await DtoTypeBuilder.GetEntityPropertiesAsync(entity.GetType()))
                .Where(p => p.Source == MetadataSourceType.UserDefined).ToList();
            var dtoProps = dto.GetType().GetProperties();
            foreach (var property in dynamicProperties)
            {
                var dtoProp = dtoProps.FirstOrDefault(p => p.Name == property.Name);
                if (dtoProp != null)
                {
                    await action.Invoke(entity, dto, property, dtoProp);
                }
            }
        }
    }
}