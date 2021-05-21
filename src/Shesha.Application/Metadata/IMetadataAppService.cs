using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using Shesha.AutoMapper.Dto;

namespace Shesha.Metadata
{
    /// <summary>
    /// Metadata application service. Provides metadata of entities, DTOs etc
    /// </summary>
    public interface IMetadataAppService: IApplicationService
    {
        /// <summary>
        /// Autocomplete of entity types
        /// </summary>
        /// <param name="term">Term to search</param>
        /// <param name="selectedValue">Selected value, is used to fetch user-friendly name of selected item</param>
        /// <returns></returns>
        Task<List<AutocompleteItemDto>> EntityTypeAutocompleteAsync(string term, string selectedValue);
    }
}
