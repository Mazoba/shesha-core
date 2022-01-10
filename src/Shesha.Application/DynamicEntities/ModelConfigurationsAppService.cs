using Abp.Application.Services;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using NHibernate.Linq;
using Shesha.Configuration.Runtime;
using Shesha.Domain;
using Shesha.Domain.Enums;
using Shesha.DynamicEntities.Dtos;
using Shesha.Elmah;
using Shesha.Utilities;
using Shesha.Web.DataTable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shesha.DynamicEntities
{
    /// <summary>
    /// Model Configurations application service
    /// </summary>
    [Route("api/ModelConfigurations")]
    public class ModelConfigurationsAppService : SheshaAppServiceBase, IApplicationService
    {
        private readonly IRepository<EntityConfig, Guid> _entityConfigRepository;
        private readonly IRepository<EntityProperty, Guid> _entityPropertyRepository;
        private readonly IModelConfigurationProvider _modelConfigurationProvider;

        public ModelConfigurationsAppService(IRepository<EntityConfig, Guid> entityConfigRepository, IRepository<EntityProperty, Guid> entityPropertyRepository, IModelConfigurationProvider modelConfigurationProvider)
        {
            _entityConfigRepository = entityConfigRepository;
            _entityPropertyRepository = entityPropertyRepository;
            _modelConfigurationProvider = modelConfigurationProvider;
        }

        [HttpGet, Route("")]
        public async Task<ModelConfigurationDto> GetByNameAsync(string name, string @namespace)
        {
            var dto = await _modelConfigurationProvider.GetModelConfigurationOrNullAsync(@namespace, name);
            if (dto == null) 
            {
                var exception = new EntityNotFoundException("Model configuration not found");
                exception.MarkExceptionAsLogged();
                throw exception;
            }

            return dto;
        }

        [HttpGet, Route("{id}")]
        public async Task<ModelConfigurationDto> GetByIdAsync(Guid id)
        {
            var modelConfig = await _entityConfigRepository.GetAll().Where(m => m.Id == id).FirstOrDefaultAsync();
            if (modelConfig == null)
            {
                var exception = new EntityNotFoundException("Model configuration not found");
                exception.MarkExceptionAsLogged();
                throw exception;
            }

            return await _modelConfigurationProvider.GetModelConfigurationAsync(modelConfig);
        }

        [HttpPut, Route("")]
        public async Task<ModelConfigurationDto> UpdateAsync(ModelConfigurationDto input)
        {
            var modelConfig = await _entityConfigRepository.GetAll().Where(m => m.Id == input.Id).FirstOrDefaultAsync();
            if (modelConfig == null)
                new EntityNotFoundException("Model configuration not found");

            // todo: add validation

            return await CreateOrUpdateAsync(modelConfig, input);
        }

        [HttpPost, Route("")]
        public async Task<ModelConfigurationDto> CreateAsync(ModelConfigurationDto input) 
        {
            var modelConfig = new EntityConfig();

            // todo: add validation

            return await CreateOrUpdateAsync(modelConfig, input);
        }

        private async Task<ModelConfigurationDto> CreateOrUpdateAsync(EntityConfig modelConfig, ModelConfigurationDto input) 
        {
            var mapper = GetModelConfigMapper(modelConfig.Source ?? Domain.Enums.MetadataSourceType.UserDefined);
            mapper.Map(input, modelConfig);
            await _entityConfigRepository.InsertOrUpdateAsync(modelConfig);

            var properties = await _entityPropertyRepository.GetAll().Where(p => p.EntityConfig == modelConfig).OrderBy(p => p.SortOrder).ToListAsync();

            var mappers = new Dictionary<MetadataSourceType, IMapper> {
                { MetadataSourceType.ApplicationCode, GetPropertyMapper(MetadataSourceType.ApplicationCode) },
                { MetadataSourceType.UserDefined, GetPropertyMapper(MetadataSourceType.UserDefined) },
            };

            await BindProperties(mappers, properties, input.Properties, modelConfig, null);

            // delete missing properties
            var allPropertiesId = new List<Guid>();
            ActionPropertiesRecursive(input.Properties, prop => {
                var id = prop.Id.ToGuidOrNull();
                if (id != null)
                    allPropertiesId.Add(id.Value);
            });
            var toDelete = properties.Where(p => !allPropertiesId.Contains(p.Id)).ToList();
            foreach (var prop in toDelete)
            {
                await _entityPropertyRepository.DeleteAsync(prop);
            }

            return await _modelConfigurationProvider.GetModelConfigurationAsync(modelConfig);
        }

        private void ActionPropertiesRecursive(List<ModelPropertyDto> properties, Action<ModelPropertyDto> action)
        {
            foreach (var property in properties) 
            {
                action.Invoke(property);
                if (property.Properties != null)
                    ActionPropertiesRecursive(property.Properties, action);
            }
        }

        private async Task BindProperties(Dictionary<MetadataSourceType, IMapper>  mappers, List<EntityProperty> allProperties, List<ModelPropertyDto> inputProperties, EntityConfig modelConfig, EntityProperty parentProperty)
        {
            var sortOrder = 0;
            foreach (var inputProp in inputProperties)
            {
                var propId = inputProp.Id.ToGuid();
                var dbProp = propId != Guid.Empty
                    ? allProperties.FirstOrDefault(p => p.Id == propId)
                    : null;
                var isNew = dbProp == null;
                if (dbProp == null)
                    dbProp = new EntityProperty
                    {
                        EntityConfig = modelConfig
                    };
                dbProp.ParentProperty = parentProperty;

                var propertyMapper = mappers[dbProp.Source ?? MetadataSourceType.UserDefined];
                propertyMapper.Map(inputProp, dbProp);

                // bind child properties
                if (inputProp.Properties != null && inputProp.Properties.Any())
                    await BindProperties(mappers, allProperties, inputProp.Properties, modelConfig, dbProp);

                dbProp.SortOrder = sortOrder++;

                await _entityPropertyRepository.InsertOrUpdateAsync(dbProp);
            }
        }

        private IMapper GetModelConfigMapper(MetadataSourceType sourceType)
        {
            var modelConfigMapperConfig = new MapperConfiguration(cfg => {
                var mapExpression = cfg.CreateMap<ModelConfigurationDto, EntityConfig>()
                    .ForMember(d => d.Id, o => o.Ignore());

                if (sourceType == MetadataSourceType.ApplicationCode)
                {
                    mapExpression.ForMember(d => d.ClassName, o => o.Ignore());
                    mapExpression.ForMember(d => d.Namespace, o => o.Ignore());
                }
            });

            return modelConfigMapperConfig.CreateMapper();
        }

        private IMapper GetPropertyMapper(MetadataSourceType sourceType) 
        {
            var propertyMapperConfig = new MapperConfiguration(cfg => {
                var mapExpression = cfg.CreateMap<ModelPropertyDto, EntityProperty>()
                    .ForMember(d => d.Id, o => o.Ignore())
                    .ForMember(d => d.EntityConfig, o => o.Ignore())
                    .ForMember(d => d.SortOrder, o => o.Ignore())
                    .ForMember(d => d.Properties, o => o.Ignore());

                if (sourceType == MetadataSourceType.ApplicationCode)
                {
                    mapExpression.ForMember(d => d.Name, o => o.Ignore());
                    mapExpression.ForMember(d => d.DataType, o => o.Ignore());
                    mapExpression.ForMember(d => d.EntityType, o => o.Ignore());
                }
            });            

            return propertyMapperConfig.CreateMapper();
        }

        private async Task<ModelConfigurationDto> GetAsync(EntityConfig modelConfig) 
        {
            var dto = ObjectMapper.Map<ModelConfigurationDto>(modelConfig);

            var properties = await _entityPropertyRepository.GetAll().Where(p => p.EntityConfig == modelConfig && p.ParentProperty == null)
                .OrderBy(p => p.SortOrder)
                .ToListAsync();

            dto.Properties = properties.Select(p => ObjectMapper.Map<ModelPropertyDto>(p)).ToList();

            return dto;
        }
    }
}
