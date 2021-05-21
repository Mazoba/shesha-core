using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using Shesha.Domain.Attributes;
using Shesha.Domain.Enums;

namespace Shesha.Domain
{
    [Entity(TypeShortAlias = "Shesha.Core.DistributionListItem")]
    public class DistributionListItem : FullAuditedEntity<Guid>, IMayHaveTenant
    {
        public virtual DistributionList DistributionList { get; set; }

        [StringLength(255)]
        public virtual string Name { get; set; }
        [StringLength(255)]
        public virtual string Email { get; set; }
        [StringLength(255)]
        public virtual string CC { get; set; }

        [StringLength(20)]
        public virtual string MobileNo { get; set; }

        public virtual Employee Person { get; set; }

        public virtual OrganisationPost Post { get; set; }

        public virtual ShaRole ShaRole { get; set; }

        public virtual OrganisationPostLevel PostLevel { get; set; }

        public virtual RefListDistributionItemType Type { get; set; }
        public virtual RefListDistributionItemSubType? SubType { get; set; }

        public virtual bool NotifyByEmail { get; set; }

        public virtual bool NotifyBySms { get; set; }

        public virtual int? TenantId { get; set; }

        public DistributionListItem()
        {
            Type = RefListDistributionItemType.System;
        }
    }
}
