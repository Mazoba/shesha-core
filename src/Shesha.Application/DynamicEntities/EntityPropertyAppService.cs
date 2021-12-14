using Abp.Application.Services;
using Abp.Domain.Repositories;
using Shesha.Domain;
using Shesha.DynamicEntities.Dtos;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shesha.DynamicEntities
{
    /// inheritedDoc
    public class EntityPropertyAppService : AsyncCrudAppService<EntityProperty, EntityPropertyDto, Guid>, IEntityPropertyAppService
    {
        public EntityPropertyAppService(IRepository<EntityProperty, Guid> repository) : base(repository)
        {
        }
    }
}
