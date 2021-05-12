using System.Collections.Generic;
using Abp.Application.Services;
using Shesha.AutoMapper.Dto;

namespace Shesha.Web.DataTable
{
    /// <summary>
    /// DataTable application service
    /// </summary>
    public interface IDataTableAppService: IApplicationService
    {
        /// <summary>
        /// Table Id autocomplete
        /// </summary>
        /// <param name="term"></param>
        /// <returns></returns>
        List<AutocompleteItemDto> TableIdAutocomplete(string term);
    }
}
