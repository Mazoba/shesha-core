using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities.Auditing;
using Shesha.Domain.Attributes;

namespace Shesha.Web.FormsDesigner.Domain
{
    /// <summary>
    /// Form
    /// </summary>
    [Entity(TypeShortAlias = "Shesha.Framework.ConfigurableComponent")]
    [Discriminator(UseDiscriminator = true)]
    [DiscriminatorValue("Shesha.Framework.ConfigurableComponent")]
    public class ConfigurableComponent : FullAuditedEntity<Guid>
    {
        /// <summary>
        /// Name
        /// </summary>
        [StringLength(100)]
        public virtual string Name { get; set; }

        /// <summary>
        /// Path
        /// </summary>
        [StringLength(300)]
        public virtual string Path { get; set; }

        /// <summary>
        /// Description
        /// </summary>
        [DataType(DataType.MultilineText)]
        [StringLength(int.MaxValue)]
        public virtual string Description { get; set; }
        
        /// <summary>
        /// Form markup
        /// </summary>
        [StringLength(int.MaxValue)]
        public virtual string Settings { get; set; }

        /// <summary>
        /// ModelType
        /// </summary>
        [StringLength(int.MaxValue)]
        public virtual string ModelType { get; set; }
    }
}
