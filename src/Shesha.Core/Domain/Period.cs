using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities.Auditing;
using Shesha.Domain.Attributes;

namespace Shesha.Domain
{
    [Entity(TypeShortAlias = "Shesha.Core.Period")]
    public class Period : FullAuditedEntity<Guid>
    {
        [StringLength(1000)]
        public virtual string Name { get; set; }
        [StringLength(100)]
        public virtual string ShortName { get; set; }
        [ReferenceList("Shesha.Core", "PeriodType")]
        public virtual int? PeriodType { get; set; }
        public virtual DateTime? PeriodStart { get; set; }
        public virtual DateTime? PeriodEnd { get; set; }
        public virtual Period ParentPeriod { get; set; }
    }
}
