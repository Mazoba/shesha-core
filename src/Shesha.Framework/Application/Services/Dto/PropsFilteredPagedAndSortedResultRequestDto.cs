using Abp.Application.Services.Dto;

namespace Shesha.Application.Services.Dto
{
    /// <summary>
    /// Filtered, pages and sorted request DTO with properties list
    /// </summary>
    public class PropsFilteredPagedAndSortedResultRequestDto : FilteredPagedAndSortedResultRequestDto
    {
        // todo: move to interface
        public string Properties { get; set; }
    }
}
