using Abp.Application.Services.Dto;
using Microsoft.AspNetCore.Mvc;

namespace Shesha.Application.Services.Dto
{
    /// <summary>
    /// Filtered, pages and sorted request DTO
    /// </summary>
    public class FilteredPagedAndSortedResultRequestDto : PagedAndSortedResultRequestDto, IFilteredPagedAndSortedResultRequestDto
    {
        /// <summary>
        /// Filter string in JsonLogic format
        /// </summary>
        public string Filter { get; set; }

        /// <summary>
        /// Quick search string. Is used to search entities by text
        /// </summary>
        public string QuickSearch { get; set; }
    }
}
