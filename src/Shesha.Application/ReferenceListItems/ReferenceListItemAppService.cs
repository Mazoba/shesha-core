using Abp.Application.Services;
using Abp.Domain.Repositories;
using Shesha.Domain;
using Shesha.Services;
using Shesha.Services.ReferenceLists.Dto;
using System;

namespace Shesha.ReferenceLists
{
    public class ReferenceListItemAppService : AsyncCrudAppService<ReferenceListItem, ReferenceListItemDto, Guid>
    {
        private readonly ReferenceListHelper _refListHelper;

        public ReferenceListItemAppService(IRepository<ReferenceListItem, Guid> repository, ReferenceListHelper refListHelper) : base(repository)
        {
            _refListHelper = refListHelper;
        }
    }
}