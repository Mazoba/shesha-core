using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Events.Bus.Entities;
using Abp.Events.Bus.Handlers;
using Abp.Runtime.Caching;
using AutoMapper;
using Shesha.Domain;
using Shesha.Extensions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shesha.DynamicEntities.Mapper
{
    public class DynamicDtoMappingHelper : IEventHandler<EntityChangedEventData<EntityProperty>>, IDynamicDtoMappingHelper, ITransientDependency
    {
        private readonly ICacheManager _cacheManager;
        private readonly IRepository<EntityProperty, Guid> _propertyRepository;
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly IocManager _iocManager;

        public ITypedCache<string, IMapper> InternalCache
        {
            get
            {
                return _cacheManager.GetCache<string, IMapper>(this.GetType().Name);
            }
        }

        public DynamicDtoMappingHelper(
            IocManager iocManager,
            ICacheManager cacheManager,
            IRepository<EntityProperty, Guid> propertyRepository,
            IUnitOfWorkManager unitOfWorkManager)
        {
            _iocManager = iocManager;
            _cacheManager = cacheManager;
            _propertyRepository = propertyRepository;
            _unitOfWorkManager = unitOfWorkManager;
        }
        
        private string GetCacheKey(Type sourceType, Type destinationType)
        {
            if (sourceType.IsEntityType())
                return GetCacheKey(sourceType.Namespace, sourceType.Name, MappingDirection.Entity2Dto);

            if (destinationType.IsEntityType())
                return GetCacheKey(destinationType.Namespace, destinationType.Name, MappingDirection.Dto2Entity);

            throw new NotSupportedException("This method supports only mapping from/to entity type");
        }

        private string GetCacheKey(string @namespace, string name, MappingDirection direction)
        {
            return $"{@namespace}.{name}:{direction}";
        }

        private List<string> GetCacheKey(EntityConfig entityConfig)
        {
            return new List<string> 
            {
                GetCacheKey(entityConfig.Namespace, entityConfig.ClassName, MappingDirection.Dto2Entity),
                GetCacheKey(entityConfig.Namespace, entityConfig.ClassName, MappingDirection.Entity2Dto),
            };
        }

        public void HandleEvent(EntityChangedEventData<EntityProperty> eventData)
        {
            if (eventData.Entity?.EntityConfig == null)
                return;

            var cacheKeys = GetCacheKey(eventData.Entity.EntityConfig);
            foreach (var cacheKey in cacheKeys) 
            {
                InternalCache.Remove(cacheKey);
            }
        }

        public async Task<IMapper> GetEntityToDtoMapperAsync(Type entityType, Type dtoType)
        {
            var cacheKey = GetCacheKey(entityType, dtoType);
            return await InternalCache.GetAsync(cacheKey, () => {
                var modelConfigMapperConfig = new MapperConfiguration(cfg =>
                {
                    var mapExpression = cfg.CreateMap(entityType, dtoType);

                    var entityMapProfile = _iocManager.Resolve<EntityMapProfile>();
                    cfg.AddProfile(entityMapProfile);

                    var reflistMapProfile = _iocManager.Resolve<ReferenceListMapProfile>();
                    cfg.AddProfile(reflistMapProfile);
                });

                return Task.FromResult(modelConfigMapperConfig.CreateMapper());
            });
        }

        public async Task<IMapper> GetDtoToEntityMapperAsync(Type entityType, Type dtoType)
        {
            var cacheKey = GetCacheKey(entityType, dtoType);
            return await InternalCache.GetAsync(cacheKey, () => {
                var modelConfigMapperConfig = new MapperConfiguration(cfg =>
                {
                    var mapExpression = cfg.CreateMap(dtoType, entityType);

                    var entityMapProfile = _iocManager.Resolve<EntityMapProfile>();
                    cfg.AddProfile(entityMapProfile);

                    var reflistMapProfile = _iocManager.Resolve<ReferenceListMapProfile>();
                    cfg.AddProfile(reflistMapProfile);
                });

                return Task.FromResult(modelConfigMapperConfig.CreateMapper());
            });
        }

        private enum MappingDirection 
        { 
            Dto2Entity,
            Entity2Dto
        }
    }
}
