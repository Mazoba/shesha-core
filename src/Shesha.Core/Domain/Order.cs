using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities.Auditing;
using Shesha.Domain.Attributes;
using Shesha.Domain.Enums;

namespace Shesha.Domain
{
    [Discriminator]
    [Entity(TypeShortAlias = "Shesha.Core.Order")]
    public class Order: FullAuditedEntity<Guid>
    {
        [StringLength(100)]
        public virtual string RequisitionNo { get; set; }
        [StringLength(100)]
        public virtual string RefNo { get; set; }
        public virtual Person Requester { get; set; }
        public virtual Person Receiver { get; set; }
        public virtual DateTime? RequestedCollectionDate { get; set; }
        public virtual DateTime? ConfirmedCollectionDate { get; set; }
        [StringLength(int.MaxValue)]
        public virtual string Comment { get; set; }
        public virtual RefListOrderStatus? Status { get; set; }
    }
}
