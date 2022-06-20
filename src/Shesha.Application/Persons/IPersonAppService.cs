using Abp.Application.Services;
using Shesha.Application.Services.Dto;
using System;

namespace Shesha.Persons
{
    /// <summary>
    /// Person Application Service
    /// </summary>
    public interface IPersonAppService: IAsyncCrudAppService<PersonAccountDto, Guid, FilteredPagedAndSortedResultRequestDto, CreatePersonAccountDto, PersonAccountDto>
    {
    }
}
