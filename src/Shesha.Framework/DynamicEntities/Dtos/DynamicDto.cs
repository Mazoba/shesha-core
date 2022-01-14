using Abp.Application.Services.Dto;
using Abp.Domain.Entities;

namespace Shesha.DynamicEntities.Dtos
{
    public class DynamicDto<TEntity, TId>: EntityDto<TId>, IDynamicDto<TEntity, TId> where TEntity: IEntity<TId>
    {

    }
}
