using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Shesha.CheckLists.Models
{
    /// <summary>
    /// Save check list selection input
    /// </summary>
    public class SaveSelectionInput
    {
        /// <summary>
        /// Owner entity Id
        /// </summary>
        [Required]
        public Guid Id { get; set; }

        /// <summary>
        /// Owner entity type short alias
        /// </summary>
        [Required]
        public string OwnerType { get; set; }

        /// <summary>
        /// Check list id
        /// </summary>
        [Required]
        public string OwnerId { get; set; }

        /// <summary>
        /// User selection
        /// </summary>
        [Required]
        public List<CheckListItemSelectionDto> Selection { get; set; } = new List<CheckListItemSelectionDto>();
    }
}
