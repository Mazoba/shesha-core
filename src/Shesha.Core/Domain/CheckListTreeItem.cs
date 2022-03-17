using System;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Shesha.Domain.Attributes;

namespace Shesha.Domain
{
    /// <summary>
    /// Check list tree item
    /// </summary>
    [Obsolete("Should use equivalent entity under Shesha.Enterprise")]
    [Table("vw_Core_CheckListTreeItems")]
    [ImMutable]
    public class CheckListTreeItem : Entity<Guid>, IMayHaveTenant
    {
        /// <summary>
        /// Name
        /// </summary>
        public virtual string Name { get; set; }

        /// <summary>
        /// Parent item Id
        /// </summary>
        public virtual Guid? ParentId { get; set; }

        /// <summary>
        /// If true, indicates that item has child items
        /// </summary>
        public virtual bool HasChilds { get; set; }

        /// <summary>
        /// Order Index
        /// </summary>
        public virtual int OrderIndex { get; set; }

        /// <summary>
        /// Tenant Id
        /// </summary>
        public virtual int? TenantId { get; set; }

        /// <summary>
        /// Check list Id
        /// </summary>
        public virtual Guid CheckListId { get; set; }

        /// <summary>
        /// Creation time
        /// </summary>
        public virtual DateTime CreationTime { get; set; }
    }
}
