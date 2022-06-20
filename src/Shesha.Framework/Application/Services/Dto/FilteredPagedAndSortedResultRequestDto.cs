using Abp.Application.Services.Dto;

namespace Shesha.Application.Services.Dto
{
    /// <summary>
    /// Filtered, pages and sorted request DTO
    /// </summary>
    public class FilteredPagedAndSortedResultRequestDto : PagedAndSortedResultRequestDto, IFilteredPagedAndSortedResultRequestDto
    {
        /// inheritedDoc
        public string Filter { get; set; }

        /// inheritedDoc
        public string QuickSearch { get; set; }
    }
}
