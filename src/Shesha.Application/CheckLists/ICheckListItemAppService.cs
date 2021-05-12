using System;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Shesha.CheckLists.Dtos;

namespace Shesha.CheckLists
{
    /// <summary>
    /// Checklist item application service
    /// </summary>
    public interface ICheckListItemAppService : IAsyncCrudAppService<CheckListItemDto, Guid, PagedAndSortedResultRequestDto, CheckListItemDto, CheckListItemDto>
    {

    }
}
