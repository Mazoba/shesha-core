using System;
using System.ComponentModel.DataAnnotations;

namespace Shesha.CheckLists.Models
{
    /// <summary>
    /// Get check list selection input
    /// </summary>
    public class GetSelectionInput
    {
        /// <summary>
        /// Check list Id
        /// </summary>
        [Required]
        public Guid Id { get; set; }

        /// <summary>
        /// Owner entity type short alias
        /// </summary>
        [Required]
        public string OwnerType { get; set; }

        /// <summary>
        /// Owner entity Id
        /// </summary>
        [Required]
        public string OwnerId { get; set; }
    }
}
