using System;
using System.Linq;
using Abp.Domain.Repositories;
using JetBrains.Annotations;
using Shesha.Domain;
using Shesha.StoredFilters.Dto;
using Shesha.StoredFilters.Dtos;

namespace Shesha.StoredFilters
{
    /// <summary>
    /// Stored filter management service
    /// </summary>
    [UsedImplicitly]
    public class StoredFilterAppService : SheshaCrudServiceBase<StoredFilter, StoredFilterDto, Guid, GetAllFiltersDto, CreateStoredFilterDto, UpdateStoredFilterDto>, IStoredFilterAppService
    {
        private readonly IRepository<StoredFilterContainer, Guid> _filterContainerRepo;

        /// <summary>
        /// 
        /// </summary>
        public StoredFilterAppService(IRepository<StoredFilter, Guid> repository, IRepository<StoredFilterContainer, Guid> filterContainerRepo): base(repository)
        {
            _filterContainerRepo = filterContainerRepo;
        }

        /// <inheritdoc/>
        protected override IQueryable<StoredFilter> CreateFilteredQuery(GetAllFiltersDto input)
        {
            return (string.IsNullOrEmpty(input.OwnerType) && string.IsNullOrEmpty(input.OwnerId)
                    // globally
                    ? Repository.GetAll()
                    // by container
                    : _filterContainerRepo
                        .GetAll().Where(c => c.OwnerId == input.OwnerId && c.OwnerType == input.OwnerType)
                        .Select(c => c.Filter))
                .OrderBy(c => c.CreationTime);
        }
    }
}
