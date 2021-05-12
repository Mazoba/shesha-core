using System;
using Abp.Application.Services.Dto;

namespace Shesha.CheckLists.Dtos
{
    /// <summary>
    /// Check list item DTO for tree component
    /// </summary>
    public class CheckListTreeItemDto : EntityDto<Guid>
    {
        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Parent Id
        /// </summary>
        public Guid? ParentId { get; set; }

        /// <summary>
        /// Order Index
        /// </summary>
        public int OrderIndex { get; set; }

        /// <summary>
        /// If true, indicates that item has child items
        /// </summary>
        public bool HasChilds { get; set; }
    }
}
