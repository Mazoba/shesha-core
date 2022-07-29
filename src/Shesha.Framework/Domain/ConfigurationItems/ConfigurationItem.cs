using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shesha.Domain.ConfigurationItems
{
    /// <summary>
    /// Configuration Item
    /// </summary>
    public class ConfigurationItem : FullPowerEntity
    {
        /// <summary>
        /// Item name
        /// </summary>
        [StringLength(200)]
        public virtual string Name { get; set; }

        /// <summary>
        /// Item name
        /// </summary>
        [StringLength(200)]
        public virtual string ItemType { get; set; }

        /// <summary>
        /// Item description
        /// </summary>
        [StringLength(int.MaxValue)]
        public virtual string Description { get; set; }
        
        /// <summary>
        /// Module
        /// </summary>
        public virtual Module Module { get; set; }

        /// <summary>
        /// Base item. Is used if the current item is inherited from another one
        /// </summary>
        public virtual ConfigurationItem BaseItem { get; set; }

        /// <summary>
        /// Version number
        /// </summary>
        public virtual int VersionNo { get; set; }

        /// <summary>
        /// Version status (Draft/In Progress/Live etc.)
        /// </summary>
        public virtual ConfigurationItemVersionStatus VersionStatus { get; set; }

        /// <summary>
        /// Parent version. Note: version may have more than one child versions (e.g. new version was created and then cancelled, in this case a new version should be created in the same parent)
        /// </summary>
        public virtual ConfigurationItem ParentVersion { get; set; }

        /// <summary>
        /// Import session that created this configuration item
        /// </summary>
        public virtual ImportResult CreatedByImport { get; set; }
    }
}
