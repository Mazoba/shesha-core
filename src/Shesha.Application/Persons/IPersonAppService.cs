using System;
using Abp.Application.Services;
using Abp.Application.Services.Dto;

namespace Shesha.Persons
{
    /// <summary>
    /// Person Application Service
    /// </summary>
    public interface IPersonAppService: IAsyncCrudAppService<PersonAccountDto, Guid, PagedAndSortedResultRequestDto, CreatePersonAccountDto, PersonAccountDto>
    {
        //Task<PersonAccountDto> CreateAccountAsync(CreatePersonAccountDto input);
    }
}
