using Abp.Dependency;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Events.Bus.Entities;
using Abp.Events.Bus.Handlers;
using Abp.ObjectMapping;
using Abp.Runtime.Caching;
using NHibernate.Linq;
using Shesha.Domain;
using Shesha.DynamicEntities.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shesha.DynamicEntities.Cache
{
    public class EntityConfigCache : IEventHandler<EntityChangedEventData<EntityProperty>>, IEntityConfigCache, ITransientDependency
    {
        private readonly ICacheManager _cacheManager;
        private readonly IRepository<EntityProperty, Guid> _propertyRepository;
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly IObjectMapper _mapper;

        public ITypedCache<string, EntityConfigCacheItem> InternalCache
        {
            get
            {
                return _cacheManager.GetCache<string, EntityConfigCacheItem>(this.GetType().Name);
            }
        }

        public EntityConfigCache(
            ICacheManager cacheManager,
            IRepository<EntityProperty, Guid> propertyRepository,
            IUnitOfWorkManager unitOfWorkManager,
            IObjectMapper mapper)
        {
            _cacheManager = cacheManager;
            _propertyRepository = propertyRepository;
            _unitOfWorkManager = unitOfWorkManager;
            _mapper = mapper;
        }
        
        private string GetPropertiesCacheKey(Type entityType)
        {
            return GetCacheKey(entityType.Namespace, entityType.Name);
        }

        private string GetCacheKey(EntityConfig entityConfig)
        {
            return GetCacheKey(entityConfig.Namespace, entityConfig.ClassName);
        }
        
        private string GetCacheKey(string @namespace, string name) 
        {
            return $"{@namespace}.{name}:properties";
        }


        public void HandleEvent(EntityChangedEventData<EntityProperty> eventData)
        {
            if (eventData.Entity?.EntityConfig == null)
                return;

            InternalCache.Remove(GetCacheKey(eventData.Entity.EntityConfig));
        }

        private async Task<List<EntityPropertyDto>> FetchPropertiesAsync(Type entityType)
        {
            using (var uow = _unitOfWorkManager.Begin())
            {
                var properties = await _propertyRepository.GetAll()
                    .Where(p => p.EntityConfig.ClassName == entityType.Name && p.EntityConfig.Namespace == entityType.Namespace && p.ParentProperty == null)
                    .ToListAsync();

                var propertyDtos = properties.Select(p => _mapper.Map<EntityPropertyDto>(p)).ToList();

                await uow.CompleteAsync();

                return propertyDtos;
            }
        }

        public async Task<List<EntityPropertyDto>> GetEntityPropertiesAsync(Type entityType)
        {
            var key = GetPropertiesCacheKey(entityType);
            var item = await InternalCache.GetAsync(key, async (key) => {
                var item = new EntityConfigCacheItem { 
                    Properties = await FetchPropertiesAsync(entityType)
                };
                return item;
            });
            
            return item.Properties;
        }
    }
}
