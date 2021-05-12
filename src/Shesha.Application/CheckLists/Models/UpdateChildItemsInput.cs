using System;
using System.Collections.Generic;

namespace Shesha.CheckLists.Models
{
    /// <summary>
    /// Move check list item input
    /// </summary>
    public class UpdateChildItemsInput
    {
        /// <summary>
        /// Id of the check list
        /// </summary>
        public Guid CheckListId { get; set; }

        /// <summary>
        /// Id of the new parent item
        /// </summary>
        public Guid? ParentId { get; set; }

        /// <summary>
        /// List of child item ids
        /// </summary>
        public List<Guid> ChildIds { get; set; } = new List<Guid>();
    }
}
