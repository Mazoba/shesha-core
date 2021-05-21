using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities.Auditing;
using Shesha.Domain.Attributes;

namespace Shesha.Domain
{
    [Discriminator]
    [Entity(TypeShortAlias = "Shesha.Core.TeamMember")]
    public class TeamMember : FullAuditedEntity<Guid>
    {
        [Required]
        public virtual Team Team { get; set; }

        [Required]
        public virtual Person Person { get; set; }

        public virtual bool Inactive { get; set; }

        [DataType(DataType.Date)]
        public virtual DateTime? ValidFromDate { get; set; }

        [DataType(DataType.Date)]
        public virtual DateTime? ValidToDate { get; set; }

        [ReferenceList("Shesha.Core", "TeamMemberRole")]
        public virtual int? Role { get; set; }
    }
}
