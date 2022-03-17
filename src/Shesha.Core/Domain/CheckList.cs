using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shesha.Domain.Attributes;

namespace Shesha.Domain
{
    /// <summary>
    /// 
    /// </summary>
    [Obsolete("Should use equivalent entity under Shesha.Enterprise")]
    [Entity(TypeShortAlias = "Core.CheckList", FriendlyName = "Check List")]
    [Table("Core_CheckLists")]
    public class CheckList : FullPowerEntity
    {
        /// <summary>
        /// Name of the check list
        /// </summary>
        [StringLength(255)]
        public virtual string Name { get; set; }

        /// <summary>
        /// Description
        /// </summary>
        [StringLength(int.MaxValue)]
        public virtual string Description { get; set; }

        /// <summary>
        /// Check list items
        /// </summary>
        public virtual IList<CheckListItem> Items { get; set; }
    }
}
