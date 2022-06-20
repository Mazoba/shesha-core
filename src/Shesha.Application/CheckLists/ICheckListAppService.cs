using Abp.Application.Services;
using Shesha.Application.Services.Dto;
using Shesha.CheckLists.Dtos;
using System;

namespace Shesha.CheckLists
{
    /// <summary>
    /// Checklist application service
    /// </summary>
    public interface ICheckListAppService : IAsyncCrudAppService<CheckListDto, Guid, FilteredPagedAndSortedResultRequestDto, CheckListDto, CheckListDto>
    {

    }
}
