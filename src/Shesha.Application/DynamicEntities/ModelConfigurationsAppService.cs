using Abp.Application.Services;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;
using NHibernate.Linq;
using Shesha.Configuration.Runtime;
using Shesha.Domain;
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
    public class ModelConfigurationsAppService : SheshaAppServiceBase, IApplicationService
    {
        private readonly IRepository<EntityConfig, Guid> _entityConfigRepository;
        private readonly IRepository<EntityProperty, Guid> _entityPropertyRepository;

        public ModelConfigurationsAppService(IRepository<EntityConfig, Guid> entityConfigRepository, IRepository<EntityProperty, Guid> entityPropertyRepository)
        {
            _entityConfigRepository = entityConfigRepository;
            _entityPropertyRepository = entityPropertyRepository;
        }

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

        [HttpGet, Route("")]
        private async Task<ModelConfigurationDto> GetAsync(EntityConfig modelConfig) 
        {
            var dto = ObjectMapper.Map<ModelConfigurationDto>(modelConfig);

            var properties = await _entityPropertyRepository.GetAll().Where(p => p.EntityConfig == modelConfig).OrderBy(p => p.SortOrder).ToListAsync();
            dto.Properties = properties.Select(p => ObjectMapper.Map<EntityPropertyDto>(p)).ToList();

            return dto;
        }
    }
}
