using Abp.Application.Services;
using Shesha.Application.Services.Dto;
using Shesha.CheckLists.Dtos;
using System;

namespace Shesha.CheckLists
{
    /// <summary>
    /// Checklist item application service
    /// </summary>
    public interface ICheckListItemAppService : IAsyncCrudAppService<CheckListItemDto, Guid, FilteredPagedAndSortedResultRequestDto, CheckListItemDto, CheckListItemDto>
    {

    }
}
