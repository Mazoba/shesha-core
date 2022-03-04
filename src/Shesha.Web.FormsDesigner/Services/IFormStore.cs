using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shesha.Web.FormsDesigner.Dtos;

namespace Shesha.Web.FormsDesigner.Services
{
    /// <summary>
    /// Interface of the form store
    /// </summary>
    public interface IFormStore
    {
        /// <summary>
        /// Get form by path
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<FormDto> GetAsync(Guid id);

        Task<FormDto> GetAsyncOrDefault(Guid id);

        /// <summary>
        /// Update form
        /// </summary>
        /// <param name="form"></param>
        /// <returns></returns>
        Task<FormDto> UpdateAsync(FormDto form);

        /// <summary>
        /// Create new form
        /// </summary>
        /// <param name="form"></param>
        /// <returns></returns>
        Task<FormDto> CreateAsync(FormDto form);


        Task<FormDto> CreateAsync(FormDto form, Guid id);

        /// <summary>
        /// Get form by path
        /// </summary>
        Task<FormDto> GetByPathAsync(string path);

        /// <summary>
        /// Autocomplete
        /// </summary>
        Task<List<FormListItemDto>> AutocompleteAsync(string term, string selectedValue);

        Task<List<FormDto>> GetAllAsync();

    }
}
