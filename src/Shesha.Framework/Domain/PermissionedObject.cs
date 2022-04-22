using System;
using Abp.Domain.Entities.Auditing;
using JetBrains.Annotations;
using Shesha.Domain.Attributes;

namespace Shesha.Domain
{
    [Entity(TypeShortAlias = "Shesha.Framework.PermissionedObject")]
    public class PermissionedObject : FullAuditedEntity<Guid>
    {
        /// <summary>
        /// Text identifier of the object (for example, the full name of the class)
        /// </summary>
        [NotNull]
        public virtual string Object { get; set; }

        /// <summary>
        /// Category for grouping objects
        /// </summary>
        [NotNull]
        public virtual string Category { get; set; }

        /// <summary>
        /// Description for display in the configurator
        /// </summary>
        public virtual string Description { get; set; }

        /// <summary>
        /// List of permissions required to access this securable (comma-separated permission identifiers)
        /// </summary>
        public virtual string Permissions { get; set; }

        /// <summary>
        /// Object inherits permissions from parent object
        /// </summary>
        public virtual bool Inherited { get; set; }

        /// <summary>
        /// Text identifier of the parent object
        /// </summary>
        public virtual string Parent { get; set; }

        /// <summary>
        /// Dependence on another permissioned object (for example, CRUD API on an entity)
        /// </summary>
        public virtual string Dependency { get; set; }

        public virtual bool Hidden { get; set; }
    }
}