using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities.Auditing;
using Shesha.Domain.Attributes;

namespace Shesha.Domain
{
    [Discriminator]
    public class FormConfiguration : FullAuditedEntity<Guid>
    {
        [StringLength(255)]
        public virtual string Name { get; set; }
    }
}
