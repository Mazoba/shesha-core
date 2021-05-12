using System;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Shesha.CheckLists.Dtos;

namespace Shesha.CheckLists
{
    /// <summary>
    /// Checklist application service
    /// </summary>
    public interface ICheckListAppService : IAsyncCrudAppService<CheckListDto, Guid, PagedAndSortedResultRequestDto, CheckListDto, CheckListDto>
    {

    }
}
