using Abp.Application.Services.Dto;
using Abp.Domain.Entities;
using Newtonsoft.Json.Linq;

namespace Shesha.DynamicEntities.Dtos
{
    public class DynamicDto<TEntity, TId> : EntityDto<TId>, IDynamicDto<TEntity, TId> where TEntity : IEntity<TId>
    {
        public JObject JObject { get; set; }
    }
}
