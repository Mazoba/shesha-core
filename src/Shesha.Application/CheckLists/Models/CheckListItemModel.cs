using System;
using System.Collections.Generic;
using Abp.Application.Services.Dto;
using Shesha.Domain.Enums;

namespace Shesha.CheckLists.Models
{
    /// <summary>
    /// Check list item model
    /// </summary>
    public class CheckListItemModel : EntityDto<Guid>
    {
        /// <summary>
        /// Item type (group = 1, two state = 2, tri state = 3), see <see cref="RefListCheckListItemType"/>
        /// </summary>
        public virtual int ItemType { get; set; }

        /// <summary>
        /// Item name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Item description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// If true, the user is able to add comments to this item/group
        /// </summary>
        public bool AllowAddComments { get; set; }

        /// <summary>
        /// Heading of the comments box
        /// </summary>
        public string CommentsHeading { get; set; }

        /// <summary>
        /// Custom visibility of comments (javascript expression)
        /// </summary>
        public string CommentsVisibilityExpression { get; set; }

        /// <summary>
        /// Child items
        /// </summary>
        public List<CheckListItemModel> ChildItems { get; set; } = new List<CheckListItemModel>();
    }
}
