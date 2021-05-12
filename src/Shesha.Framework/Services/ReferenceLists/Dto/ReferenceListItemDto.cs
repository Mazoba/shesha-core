using System;
using Abp.Application.Services.Dto;
using Shesha.AutoMapper.Dto;
using Shesha.Domain;

namespace Shesha.Services.ReferenceLists.Dto
{
    /// <summary>
    /// Dto of the <see cref="ReferenceListItem"/>
    /// </summary>
    public class ReferenceListItemDto: EntityDto<Guid>
    {
        public string Item { get; set; }
        public Int64 ItemValue { get; set; }
        public string Description { get; set; }
        public Int64 OrderIndex { get; set; }
        public EntityWithDisplayNameDto<Guid?> ReferenceList { get; set; }
    }
}
