using System;

namespace Shesha.CheckLists.Models
{
    /// <summary>
    /// Input to get child check list items
    /// </summary>
    public class GetChildCheckListItemsInput
    {
        /// <summary>
        /// Id of the checklist
        /// </summary>
        public Guid CheckListId { get; set; }

        /// <summary>
        /// Id of the parent item
        /// </summary>
        public Guid? ParentId { get; set; }
    }
}
