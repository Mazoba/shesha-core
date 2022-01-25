using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Abp.Runtime.Validation;
using Microsoft.AspNetCore.Mvc;
using Shesha.AutoMapper.Dto;
using Shesha.Configuration.Runtime;
using Shesha.DynamicEntities;
using Shesha.Metadata.Dtos;

namespace Shesha.Metadata
{
    /// inheritedDoc
    public class MetadataAppService : IMetadataAppService
    {
        private readonly IEntityConfigurationStore _entityConfigurationStore;
        private readonly IMetadataProvider _metadataProvider;
        private readonly IModelConfigurationProvider _modelConfigurationProvider;

        public MetadataAppService(IEntityConfigurationStore entityConfigurationStore, IMetadataProvider metadataProvider, IModelConfigurationProvider modelConfigurationProvider)
        {
            _entityConfigurationStore = entityConfigurationStore;
            _metadataProvider = metadataProvider;
            _modelConfigurationProvider = modelConfigurationProvider;
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

            var entities = isPreselection
                ? _entityConfigurationStore.EntityTypes.Where(e => e.Key == selectedValue).ToList()
                : _entityConfigurationStore.EntityTypes
                .Where(e => string.IsNullOrWhiteSpace(term) || 
                    e.Key.Contains(term, StringComparison.InvariantCultureIgnoreCase) || 
                    e.Value.Name.Contains(term, StringComparison.InvariantCultureIgnoreCase))
                .OrderBy(e => e.Value.Name)
                .Take(10)
                .ToList();

            var result = entities
                .Select(e => new AutocompleteItemDto
                {
                    DisplayText = $"{e.Value.Name} ({e.Key})",
                    Value = e.Key
                })
                .ToList();

            return result;
        }

        /// inheritedDoc
        [HttpGet]
        public async Task<List<PropertyMetadataDto>> PropertyAutocompleteAsync(string term, string container, string selectedValue)
        {
            if (string.IsNullOrWhiteSpace(container))
                throw new AbpValidationException($"'{nameof(container)}' is mandatory");

            var containerType = _entityConfigurationStore.EntityTypes.ContainsKey(container)
                ? _entityConfigurationStore.EntityTypes[container]
                : null;
            if (containerType == null)
                return new List<PropertyMetadataDto>();

            var flags = BindingFlags.Public | BindingFlags.Instance;
            //if (config.HideInherited)
            //    flags = flags | BindingFlags.DeclaredOnly;

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

            var containerType = _entityConfigurationStore.EntityTypes.ContainsKey(container)
                ? _entityConfigurationStore.EntityTypes[container]
                : null;
            if (containerType == null)
                return new List<PropertyMetadataDto>();

            var flags = BindingFlags.Public | BindingFlags.Instance;
            //if (config.HideInherited)
            //    flags = flags | BindingFlags.DeclaredOnly;

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
