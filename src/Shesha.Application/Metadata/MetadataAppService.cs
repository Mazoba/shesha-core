using Abp.Runtime.Validation;
using Microsoft.AspNetCore.Mvc;
using Shesha.AutoMapper.Dto;
using Shesha.Configuration.Runtime;
using Shesha.DynamicEntities;
using Shesha.Metadata.Dtos;
using Shesha.Metadata.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Shesha.Metadata
{
    /// inheritedDoc
    public class MetadataAppService : SheshaAppServiceBase, IMetadataAppService
    {
        private readonly IEntityConfigurationStore _entityConfigurationStore;
        private readonly IMetadataProvider _metadataProvider;
        private readonly IModelConfigurationProvider _modelConfigurationProvider;
        private readonly IEnumerable<IModelProvider> _modelProviders;

        public MetadataAppService(IEntityConfigurationStore entityConfigurationStore, IMetadataProvider metadataProvider, IModelConfigurationProvider modelConfigurationProvider, IEnumerable<IModelProvider> modelProviders)
        {
            _entityConfigurationStore = entityConfigurationStore;
            _metadataProvider = metadataProvider;
            _modelConfigurationProvider = modelConfigurationProvider;
            _modelProviders = modelProviders;
        }

        private async Task<List<ModelDto>> GetAllModelsAsync()
        {
            var models = new List<ModelDto>();
            foreach (var provider in _modelProviders) 
            {
                models.AddRange(await provider.GetModelsAsync());
            }
            return models;
        }

        [HttpGet]
        public Task<List<AutocompleteItemDto>> TypeAutocompleteAsync(string term, string selectedValue) 
        {
            // note: temporary return only entities
            return EntityTypeAutocompleteAsync(term, selectedValue);
        }

        /// inheritedDoc
        [HttpGet]
        public async Task<List<AutocompleteItemDto>> EntityTypeAutocompleteAsync(string term, string selectedValue)
        {
            var isPreselection = string.IsNullOrWhiteSpace(term) && !string.IsNullOrWhiteSpace(selectedValue);
            var models = await GetAllModelsAsync();

            var entities = isPreselection
                ? models.Where(e => e.ClassName == selectedValue || e.Alias == selectedValue).ToList()
                : models
                .Where(e => string.IsNullOrWhiteSpace(term) ||
                    !string.IsNullOrWhiteSpace(e.Alias) && e.Alias.Contains(term, StringComparison.InvariantCultureIgnoreCase) ||
                    e.ClassName.Contains(term, StringComparison.InvariantCultureIgnoreCase))
                .OrderBy(e => e.ClassName)
                .Take(10)
                .ToList();

            var result = entities
                .Select(e => new AutocompleteItemDto
                {
                    DisplayText = !string.IsNullOrWhiteSpace(e.Alias)
                        ? $"{e.ClassName} ({e.Alias})"
                        : e.ClassName,
                    Value = !string.IsNullOrWhiteSpace(e.Alias) 
                        ? e.Alias 
                        : e.ClassName
                })
                .ToList();

            return result;
        }

        private async Task<Type> GetContainerTypeAsync(string container) 
        {
            var allModels = await GetAllModelsAsync();
            var models = allModels.Where(m => m.Alias == container || m.ClassName == container).ToList();

            if (models.Count() > 1)
                throw new DuplicateModelsException(models);

            return models.FirstOrDefault()?.Type;
        }

        /// inheritedDoc
        [HttpGet]
        public async Task<List<PropertyMetadataDto>> PropertyAutocompleteAsync(string term, string container, string selectedValue)
        {
            if (string.IsNullOrWhiteSpace(container))
                throw new AbpValidationException($"'{nameof(container)}' is mandatory");

            var containerType = await GetContainerTypeAsync(container);

            if (containerType == null)
                return new List<PropertyMetadataDto>();

            var flags = BindingFlags.Public | BindingFlags.Instance;

            var allProps = containerType.GetProperties(flags);

            var allPropsMetadata = allProps.Select(p => _metadataProvider.GetPropertyMetadata(p)).ToList();

            var result = allPropsMetadata
                .Where(e => string.IsNullOrWhiteSpace(term) || e.Path.Contains(term, StringComparison.InvariantCultureIgnoreCase))
                .OrderBy(e => e.Path)
                .Take(10)
                .ToList();

            return result;
        }

        /// inheritedDoc
        [HttpGet]
        public async Task<List<PropertyMetadataDto>> GetPropertiesAsync(string container)
        {
            if (string.IsNullOrWhiteSpace(container))
                throw new AbpValidationException($"'{nameof(container)}' is mandatory");

            var containerType = await GetContainerTypeAsync(container);

            if (containerType == null)
                return new List<PropertyMetadataDto>();

            var flags = BindingFlags.Public | BindingFlags.Instance;

            var hardCodedProps = containerType.GetProperties(flags)
                .Select(p => _metadataProvider.GetPropertyMetadata(p))
                .OrderBy(e => e.Path)
                .ToList();

            // try to get data-driven configuration
            var modelConfig = await _modelConfigurationProvider.GetModelConfigurationOrNullAsync(containerType.Namespace, containerType.Name);
            if (modelConfig != null) 
            {
                var idx = 0;
                return modelConfig.Properties
                    .Select(p => {
                        var hardCodedProp = hardCodedProps.FirstOrDefault(pp => pp.Path == p.Name);

                        return new PropertyMetadataDto
                        {

                            IsVisible = hardCodedProp?.IsVisible ?? true,
                            Required = hardCodedProp?.Required ?? false,
                            Readonly = hardCodedProp?.Readonly ?? false,
                            MinLength = hardCodedProp?.MinLength,
                            MaxLength = hardCodedProp?.MaxLength,
                            Min = hardCodedProp?.Min,
                            Max = hardCodedProp?.Max,
                            EnumType = hardCodedProp?.EnumType,
                            OrderIndex = idx++,
                            GroupName = hardCodedProp?.GroupName,

                            Path = p.Name,
                            Label = p.Label,
                            Description = p.Description,
                            DataType = p.DataType,
                            DataFormat = p.DataFormat,
                            EntityTypeShortAlias = p.EntityType,
                            ReferenceListName = p.ReferenceListName,
                            ReferenceListNamespace = p.ReferenceListNamespace,
                            IsFrameworkRelated = p.IsFrameworkRelated,
                            Properties = p.Properties.Select(pp => ObjectMapper.Map<PropertyMetadataDto>(pp)).ToList()
                        };
                    })
                    .ToList();
            } else
                return hardCodedProps;
        }

        /*
         * back-end:
         * todo: Cache current level of hierarchy (properties of Person)
         * todo: support nested objects Person.AreaLevel1.Name
         
         * front-end:
         * todo: create new component - property autocomplete that uses MetadataProvider
        
        * designer:
        * todo: separate providers for designer and form
        * on the designer level add a new property - data context
        
        Add property - isContainer
        PropertiesLoaded

         */
    }
}
