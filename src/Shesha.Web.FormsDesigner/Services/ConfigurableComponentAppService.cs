using Abp.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Shesha.Elmah;
using Shesha.Web.FormsDesigner.Dtos;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Shesha.Web.FormsDesigner.Services
{
    /// <summary>
    /// Configurable components application service
    /// </summary>
    [Route("api/services/ConfigurableComponents")]
    public class ConfigurableComponentAppService : SheshaAppServiceBase, IConfigurableComponentAppService
    {
        private readonly IConfigurableComponentStore _componentStore;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="componentStore"></param>
        public ConfigurableComponentAppService(IConfigurableComponentStore componentStore)
        {
            _componentStore = componentStore;
        }

        /// inheritedDoc
        [HttpGet, Route("{id}")]
        public async Task<ConfigurableComponentDto> GetAsync(Guid id)
        {
            try
            {
                var component = await _componentStore.GetAsync(id);
                return component;
            }
            catch (EntityNotFoundException e) 
            {
                // prevent exception logging, 
                e.MarkExceptionAsLogged();
                throw;
            }
        }

        /// inheritedDoc
        [HttpPut, Route("")]
        public async Task<ConfigurableComponentDto> UpdateAsync(ConfigurableComponentDto dto)
        {
            return await _componentStore.UpdateAsync(dto);
        }

        /// inheritedDoc
        [HttpPost, Route("")]
        public async Task<ConfigurableComponentDto> CreateAsync(ConfigurableComponentDto dto)
        {
            var result = await _componentStore.CreateAsync(dto);
            return result;
        }

        /// inheritedDoc
        [HttpPut, Route("{id}/Settings")]
        public async Task UpdateSettingsAsync(ConfigurableComponentUpdateSettingsInput input)
        {
            var component = await _componentStore.GetOrCreateAsync(input.Id);

            component.Settings = input.Settings;
            await _componentStore.UpdateAsync(component);

            // update physical json file on disk
            // Note: it's only for development, to be removed later!
            // I use this just to simplify component properties
            if (!string.IsNullOrWhiteSpace(component.Path))
            {
                try
                {
                    if (File.Exists(component.Path))
                    {
                        await File.WriteAllTextAsync(component.Path, input.Settings);
                    }
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to save component json to file", e);
                }
            }
        }
    }
}
