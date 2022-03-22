using System;
using System.ComponentModel.DataAnnotations;
using Abp.Auditing;
using Abp.Domain.Entities.Auditing;
using Shesha.Domain.Attributes;

namespace Shesha.Domain
{
    [Entity(TypeShortAlias = "Shesha.Core.PublicHoliday")]
    [Audited]
    public class PublicHoliday : FullAuditedEntity<Guid>
    {
        [DataType(DataType.Date)]
        [Required]
        public virtual DateTime? Date { get; set; }

        [Required(AllowEmptyStrings = false), StringLength(300)]
        public virtual string Name { get; set; }
    }
}
