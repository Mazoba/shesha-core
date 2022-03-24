using System;
using Abp.Domain.Entities.Auditing;
using JetBrains.Annotations;
using Shesha.Domain.Attributes;

namespace Shesha.Domain
{
    [Entity(TypeShortAlias = "Shesha.Framework.ProtectedObject")]
    public class ProtectedObject : FullAuditedEntity<Guid>
    {
        [NotNull]
        public virtual string Object { get; set; }

        [NotNull]
        public virtual string Category { get; set; }

        public virtual string Description { get; set; }

        public virtual string Permissions { get; set; }

        public virtual bool Inherited { get; set; }

        public virtual string Parent { get; set; }

        public virtual string Dependency { get; set; }

        public virtual bool Hidden { get; set; }
    }
}