using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using Shesha.Domain.Attributes;

namespace Shesha.Domain
{
    [Entity(TypeShortAlias = "Shesha.Core.DistributionList")]
    public class DistributionList : FullAuditedEntity<Guid>, IMayHaveTenant
    {
        [StringLength(255)]
        public virtual string Name { get; set; }
        [StringLength(255)]
        public virtual string Namespace { get; set; }
        public virtual int? TenantId { get; set; }
    }
}