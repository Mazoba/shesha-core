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

        public ModelConfigurationsAppService(IRepository<EntityConfig, Guid> entityConfigRepository, IRepository<EntityProperty, Guid> entityPropertyRepository)
        {
            _entityConfigRepository = entityConfigRepository;
            _entityPropertyRepository = entityPropertyRepository;
        }

        [HttpGet, Route("")]
        public async Task<ModelConfigurationDto> GetByNameAsync(string name, string @namespace)
        {
            var modelConfig = await _entityConfigRepository.GetAll().Where(m => m.ClassName == name && m.Namespace == @namespace).FirstOrDefaultAsync();
            if (modelConfig == null) 
            {
                var exception = new EntityNotFoundException("Model configuration not found");
                exception.MarkExceptionAsLogged();
                throw exception;
            }

            return await GetAsync(modelConfig);
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

            return await GetAsync(modelConfig);
        }

        [HttpPost, Route("")]
        public async Task<ModelConfigurationDto> UpdateAsync(ModelConfigurationDto input)
        {
            var modelConfig = await _entityConfigRepository.GetAll().Where(m => m.Id == input.Id).FirstOrDefaultAsync();
            if (modelConfig == null)
                new EntityNotFoundException("Model configuration not found");

            // update ClassName and Namespace only for user defined models
            if (modelConfig.Source == Domain.Enums.MetadataSourceType.UserDefined) 
            {
                modelConfig.ClassName = input.ClassName;
                modelConfig.Namespace = input.Namespace;
            }
            await _entityConfigRepository.UpdateAsync(modelConfig);            

            var properties = await _entityPropertyRepository.GetAll().Where(p => p.EntityConfig == modelConfig).OrderBy(p => p.SortOrder).ToListAsync();
            
            var toDelete = properties.Where(p => !input.Properties.Any(ip => ip.Id == p.Id)).ToList();
            foreach (var prop in toDelete) 
            {
                await _entityPropertyRepository.DeleteAsync(prop);
            }

            var mappers = new Dictionary<MetadataSourceType, IMapper> {
                { MetadataSourceType.ApplicationCode, GetPropertyMapper(MetadataSourceType.ApplicationCode) },
                { MetadataSourceType.UserDefined, GetPropertyMapper(MetadataSourceType.UserDefined) },
            };

            var sortOrder = 0;
            foreach (var inputProp in input.Properties) 
            {
                var dbProp = properties.FirstOrDefault(p => p.Id == inputProp.Id);
                var isNew = dbProp == null;
                if (dbProp == null)
                    dbProp = new EntityProperty 
                    {
                        EntityConfig = modelConfig
                    };

                var propertyMapper = mappers[dbProp.Source ?? MetadataSourceType.UserDefined];
                propertyMapper.Map(inputProp, dbProp);

                dbProp.SortOrder = sortOrder++;

                await _entityPropertyRepository.InsertOrUpdateAsync(dbProp);
            }

            return await GetAsync(modelConfig);
        }

        private IMapper GetPropertyMapper(MetadataSourceType sourceType) 
        {
            var propertyMapperConfig = new MapperConfiguration(cfg => {
                var mapExpression = cfg.CreateMap<EntityPropertyDto, EntityProperty>()
                    .ForMember(d => d.Id, o => o.Ignore())
                    .ForMember(d => d.EntityConfig, o => o.Ignore())
                    .ForMember(d => d.SortOrder, o => o.Ignore());

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

            var properties = await _entityPropertyRepository.GetAll().Where(p => p.EntityConfig == modelConfig).OrderBy(p => p.SortOrder).ToListAsync();
            dto.Properties = properties.Select(p => ObjectMapper.Map<EntityPropertyDto>(p)).ToList();

            return dto;
        }
    }
}
