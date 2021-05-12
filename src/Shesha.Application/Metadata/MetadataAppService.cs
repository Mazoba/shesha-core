using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Shesha.AutoMapper.Dto;
using Shesha.Configuration.Runtime;

namespace Shesha.Metadata
{
    /// inheritedDoc
    public class MetadataAppService : IMetadataAppService
    {
        private readonly IEntityConfigurationStore _entityConfigurationStore;

        public MetadataAppService(IEntityConfigurationStore entityConfigurationStore)
        {
            _entityConfigurationStore = entityConfigurationStore;
        }

        /// inheritedDoc
        [HttpGet]
        public async Task<List<AutocompleteItemDto>> EntityTypeAutocompleteAsync(string term, string selectedValue)
        {
            var entities = _entityConfigurationStore.EntityTypes
                .Where(e => string.IsNullOrWhiteSpace(term) || e.Key.Contains(term, StringComparison.InvariantCultureIgnoreCase) || e.Value.Name.Contains(term, StringComparison.InvariantCultureIgnoreCase))
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
    }
}
