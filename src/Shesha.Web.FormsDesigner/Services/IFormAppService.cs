using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Shesha.AutoMapper.Dto;
using Shesha.Web.FormsDesigner.Dtos;

namespace Shesha.Web.FormsDesigner.Services
{
    /// <summary>
    /// Configurable forms application service
    /// </summary>
    public interface IFormAppService
    {
        /// <summary>
        /// Get form
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<FormDto> GetAsync(Guid id);

        /// <summary>
        /// Get form by path
        /// </summary>
        Task<FormDto> GetByPathAsync(string path);

        /// <summary>
        /// Update form
        /// </summary>
        /// <returns></returns>
        Task<FormDto> UpdateAsync(FormDto form);

        /// <summary>
        /// Create new form
        /// </summary>
        /// <returns></returns>
        Task<FormDto> CreateAsync(FormDto form);

        /// <summary>
        /// Update form markup
        /// </summary>
        /// <returns></returns>
        Task UpdateMarkupAsync(FormUpdateMarkupInput input);

        /// <summary>
        /// Autocomplete by name
        /// </summary>
        /// <param name="term"></param>
        /// <param name="selectedValue"></param>
        /// <returns></returns>
        Task<List<AutocompleteItemDto>> AutocompleteAsync(string term, string selectedValue);

    }
}
