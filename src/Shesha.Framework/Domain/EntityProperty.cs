using Abp.Domain.Entities.Auditing;
using Shesha.Domain.Attributes;
using Shesha.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Shesha.Domain
{
    /// <summary>
    /// Configuration of the entity property
    /// </summary>
    [Entity(TypeShortAlias = "Shesha.Framework.EntityProperty")]
    public class EntityProperty: FullAuditedEntity<Guid>
    {
        /// <summary>
        /// Owner entity config
        /// </summary>
        public virtual EntityConfig EntityConfig { get; set; }

        /// <summary>
        /// Property Name
        /// </summary>
        public virtual string Name { get; set; }

        /// <summary>
        /// Label (display name)
        /// </summary>
        [StringLength(300)]
        public virtual string Label { get; set; }

        /// <summary>
        /// Description
        /// </summary>
        [DataType(System.ComponentModel.DataAnnotations.DataType.MultilineText)]
        public virtual string Description { get; set; }

        /// <summary>
        /// Data type
        /// </summary>
        [StringLength(100)]
        public virtual string DataType { get; set; }

        /// <summary>
        /// Data format
        /// </summary>
        [StringLength(100)]
        public virtual string DataFormat { get; set; }

        /// <summary>
        /// Entity type. Aplicable for entity references
        /// </summary>
        [StringLength(300)]
        public virtual string EntityType { get; set; }

        /// <summary>
        /// Reference list name
        /// </summary>
        [StringLength(100)]
        public virtual string ReferenceListName { get; set; }

        /// <summary>
        /// Reference list namespace
        /// </summary>
        [StringLength(300)]
        public virtual string ReferenceListNamespace { get; set; }
        
        /// <summary>
        /// Source of the property (code/user)
        /// </summary>
        public virtual MetadataSourceType? Source { get; set; }

        /// <summary>
        /// Default sort order
        /// </summary>
        public virtual int? SortOrder { get; set; }

        /// <summary>
        /// Parent property
        /// </summary>
        public virtual EntityProperty ParentProperty { get; set; }

        /// <summary>
        /// Child properties
        /// </summary>
        [InverseProperty("ParentPropertyId")]
        public virtual IList<EntityProperty> Properties { get; set; }

        public EntityProperty()
        {
            // set to user-defined by default, `ApplicationCode` is used in the bootstrapper only
            Source = MetadataSourceType.UserDefined;
        }
    }
}
