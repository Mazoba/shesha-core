using System;
using Abp.AutoMapper;
using Shesha.Domain;
using Shesha.Domain.Enums;

namespace Shesha.CheckLists.Models
{
    /// <summary>
    /// Check list item selection made by the user
    /// </summary>
    [AutoMap(typeof(CheckListItemSelection))]
    public class CheckListItemSelectionDto
    {
        /// <summary>
        /// Check list item id
        /// </summary>
        public Guid CheckListItemId { get; set; }

        /// <summary>
        /// User selection (yes = 1, no = 2, na = 3), see <see cref="RefListCheckListSelectionType"/>
        /// </summary>
        public int? Selection { get; set; }


        /// <summary>
        /// User comments
        /// </summary>
        public string Comments { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Name { get; set; }
    }
}
