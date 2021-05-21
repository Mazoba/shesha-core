using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using Shesha.Domain.Attributes;

namespace Shesha.Domain
{
    [Entity(TypeShortAlias = "Shesha.Core.Service")]
    public class Service : FullAuditedEntity<Guid>, IMayHaveTenant
    {
        [Required(AllowEmptyStrings = false), StringLength(200)]
        [EntityDisplayName]
        public virtual string ServiceName { get; set; }

        [ReferenceList("Shesha.Core", "ServiceCategory")]
        public virtual int? ServiceCategory { get; set; }

        [StringLength(300)]
        public virtual string Description { get; set; }

        [StringLength(300)]
        public virtual string Comments { get; set; }

        public virtual int? TenantId { get; set; }
    }
}
