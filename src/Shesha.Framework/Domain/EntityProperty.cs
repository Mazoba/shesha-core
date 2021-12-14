using Abp.Domain.Entities.Auditing;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shesha.Domain
{
    /// <summary>
    /// Configuration of the entity property
    /// </summary>
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
        public virtual string Label { get; set; }

        /// <summary>
        /// Description
        /// </summary>
        public virtual string Description { get; set; }
        
        /// <summary>
        /// Data type
        /// </summary>
        public virtual string DataType { get; set; }

        /// <summary>
        /// Entity type. Aplicable for entity references
        /// </summary>
        public virtual string EntityType { get; set; }
        
        /// <summary>
        /// Reference list name
        /// </summary>
        public virtual string ReferenceListName { get; set; }
        
        /// <summary>
        /// Reference list namespace
        /// </summary>
        public virtual string ReferenceListNamespace { get; set; }
    }
}
