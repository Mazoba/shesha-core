using Abp.Domain.Entities;
using Shesha.Domain.Attributes;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shesha.Domain
{
    /// <summary>
    /// Flattened Reference List items
    /// </summary>
    [Table("vw_Frwk_FlatReferenceListItems")]
    [ImMutable]
    [Entity(GenerateApplicationService = false)]
    public class FlatReferenceListItem: Entity<Guid>, IMayHaveTenant
    {
        /// <summary>
        /// Full name of the reference list in dot notation
        /// </summary>
        public virtual string ReferenceListFullName { get; set; }
        
        /// <summary>
        /// Item text
        /// </summary>
        public virtual string Item { get; set; }

        /// <summary>
        /// Item value
        /// </summary>
        public virtual Int64 ItemValue { get; set; }

        /// <summary>
        /// Tenant id
        /// </summary>
        public virtual int? TenantId { get; set; }
    }
}
