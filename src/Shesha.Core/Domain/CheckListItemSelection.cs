using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shesha.Domain.Enums;

namespace Shesha.Domain
{
    /// <summary>
    /// Check list item selection (value selected by the user)
    /// </summary>
    [Table("Core_CheckListItemSelections")]
    public class CheckListItemSelection: FullPowerManyToManyLinkEntity
    {
        /// <summary>
        /// Check list item
        /// </summary>
        public virtual CheckListItem CheckListItem { get; set; }

        /// <summary>
        /// Value selected by the user
        /// </summary>
        public virtual RefListCheckListSelectionType? Selection { get; set; }

        /// <summary>
        /// User comments
        /// </summary>
        [StringLength(int.MaxValue)]
        [DataType(DataType.MultilineText)]
        public virtual string Comments { get; set; }
    }
}
