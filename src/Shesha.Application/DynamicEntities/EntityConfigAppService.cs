using Abp.Domain.Repositories;
using Shesha.Domain;
using Shesha.DynamicEntities.Dtos;
using System;

namespace Shesha.DynamicEntities;

/// inheritedDoc
public class EntityConfigAppService : SheshaCrudServiceBase<EntityConfig, EntityConfigDto, Guid>, IEntityConfigAppService
{
    public EntityConfigAppService(IRepository<EntityConfig, Guid> repository) : base(repository)
    {
    }
}