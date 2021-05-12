using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shesha.Domain.Attributes;
using Shesha.Domain.Enums;

namespace Shesha.Domain
{
    /// <summary>
    /// Check list item
    /// </summary>
    [Entity(TypeShortAlias = "Core.CheckListItem")]
    [Discriminator(UseDiscriminator = false)]
    [Table("Core_CheckListItems")]
    public class CheckListItem : FullPowerEntity
    {
        /// <summary>
        /// Check list which current item belongs to
        /// </summary>
        public virtual CheckList CheckList { get; set; }

        /// <summary>
        /// Parent item
        /// </summary>
        public virtual CheckListItem Parent { get; set; }
        
        /// <summary>
        /// Order index of the item
        /// </summary>
        public virtual int OrderIndex { get; set; }

        /// <summary>
        /// Item type (group/two state/tri state)
        /// </summary>
        public virtual RefListCheckListItemType ItemType { get; set; }

        /// <summary>
        /// Item name
        /// </summary>
        [StringLength(255)]
        [Required]
        public virtual string Name { get; set; }

        /// <summary>
        /// Item description
        /// </summary>
        [DataType(DataType.Html)]
        [StringLength(int.MaxValue)]
        public virtual string Description { get; set; }

        /// <summary>
        /// If true, the user is able to add comments to this item/group
        /// </summary>
        public virtual bool AllowAddComments { get; set; }

        /// <summary>
        /// Heading of the comments box
        /// </summary>
        [StringLength(255)]
        public virtual string CommentsHeading { get; set; }

        /// <summary>
        /// Custom visibility of comments (javascript expression)
        /// </summary>
        [DataType(DataType.MultilineText)]
        [StringLength(int.MaxValue)]
        public virtual string CommentsVisibilityExpression { get; set; }
    }
}
