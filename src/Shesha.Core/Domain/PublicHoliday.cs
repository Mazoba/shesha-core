using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities.Auditing;
using Shesha.Domain.Attributes;

namespace Shesha.Domain
{
    [Obsolete("Should use equivalent entity under Shesha.Enterprise")]
    [Entity(TypeShortAlias = "Shesha.Core.PublicHoliday")]
    public class PublicHoliday : FullAuditedEntity<Guid>
    {
        [DataType(DataType.Date)]
        [Required]
        public virtual DateTime? Date { get; set; }

        [Required(AllowEmptyStrings = false), StringLength(300)]
        public virtual string Name { get; set; }
    }
}
