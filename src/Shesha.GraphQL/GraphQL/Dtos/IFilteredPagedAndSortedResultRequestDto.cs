using Abp.Application.Services.Dto;

namespace Shesha.GraphQL.Dtos
{
    /// <summary>
    /// Standard request of a filtered, paged and sorted list.
    /// </summary>
    public interface IFilteredPagedAndSortedResultRequestDto: IPagedAndSortedResultRequest
    {
        /// <summary>
        /// Filter string in JsonLogic format
        /// </summary>
        string Filter { get; set; }
    }
}
