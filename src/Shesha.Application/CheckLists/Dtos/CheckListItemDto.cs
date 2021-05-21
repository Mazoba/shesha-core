using System;
using Abp.Application.Services.Dto;
using Shesha.AutoMapper.Dto;

namespace Shesha.CheckLists.Dtos
{
    /// <summary>
    /// CheckListItem DTO
    /// </summary>
    public class CheckListItemDto : EntityDto<Guid>
    {
        /// <summary>
        /// Id of the check list which current item belongs to
        /// </summary>
        public Guid CheckListId { get; set; }

        /// <summary>
        /// Parent item id
        /// </summary>
        public Guid? ParentId { get; set; }

        /// <summary>
        /// Order index of the item
        /// </summary>
        public int OrderIndex { get; set; }

        /// <summary>
        /// Item type (group/two state/tri state)
        /// </summary>
        public ReferenceListItemValueDto ItemType { get; set; }

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
    }
}
