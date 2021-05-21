using System;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Shesha.StoredFilters.Dto;
using Shesha.StoredFilters.Dtos;

namespace Shesha.StoredFilters
{
    /// <summary>
    /// Stored filter management interface
    /// </summary>
    public interface IStoredFilterAppService : IAsyncCrudAppService<StoredFilterDto, Guid, GetAllFiltersDto, CreateStoredFilterDto, UpdateStoredFilterDto>
    {
    }
}
