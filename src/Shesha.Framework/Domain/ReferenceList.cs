using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using Shesha.Domain.Attributes;

namespace Shesha.Domain
{
    [Entity(TypeShortAlias = "Shesha.Framework.ReferenceList")]
    public class ReferenceList : FullAuditedEntity<Guid>, IMayHaveTenant
    {
        public ReferenceList()
        {
            NoSelectionValue = -999;
        }

        [Required(AllowEmptyStrings = false), StringLength(300)]
        public virtual string Name { get; set; }

        [StringLength(300)]
        public virtual string Description { get; set; }

        /// <summary>
        /// If true indicates that the application logic references
        /// the values or names of the items directly and should therefore
        /// not be changed once set.
        /// </summary>
        [Display(Name = "Has hard link to application", Description = "If true, indicates that the application logic references the values or names of the items directly and should therefore not be changed once set.")]
        public virtual bool HardLinkToApplication { get; set; }

        [Required(AllowEmptyStrings = false), StringLength(300)]
        public virtual string Namespace { get; set; }

        [Display(Name = "No Selection Value")]
        public virtual int? NoSelectionValue { get; set; }

        /*
        /// <summary>
        /// If true, the numbering of the reference list items should
        /// follow a binary sequence i.e. 1, 2, 4, 8, etc... This is typiclly the case where a reference
        /// list is used for a Multi-value reference list property.
        /// </summary>
        [Display(Name = "Must use bit numbering", Description = "If true, the numbering of the reference list items should follow a binary sequence i.e. 1, 2, 4, 8, etc... This is typiclly the case where a reference list is used for a Multi-value reference list property.")]
        public virtual bool MustUseBitNumbering { get; set; }         
         */

        public virtual int? TenantId { get; set; }
    }
}