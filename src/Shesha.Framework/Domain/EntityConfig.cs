using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities.Auditing;
using Shesha.Domain.Attributes;

namespace Shesha.Domain
{
    /// <summary>
    /// Entity configuration
    /// </summary>
    [Entity(TypeShortAlias = "Shesha.Framework.EntityConfig")]
    public class EntityConfig : FullAuditedEntity<Guid>
    {
        [EntityDisplayName]
        [StringLength(255)]
        public virtual string FriendlyName { get; set; }
        [StringLength(100)]
        public virtual string TypeShortAlias { get; set; }
        [StringLength(255)]
        public virtual string TableName { get; set; }
        [StringLength(500)]
        public virtual string ClassName { get; set; }
        [StringLength(500)]
        public virtual string Namespace { get; set; }
        [StringLength(255)]
        public virtual string DiscriminatorValue { get; set; }
    }
}
