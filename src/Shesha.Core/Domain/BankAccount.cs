using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using Shesha.Domain.Attributes;

namespace Shesha.Domain
{
    [Entity(TypeShortAlias = "Shesha.Core.BankAccount")]
    public class BankAccount : FullAuditedEntity<Guid>, IMayHaveTenant
    {
        [StringLength(100)]
        public virtual string AccountHolderName { get; set; }

        [ReferenceList("Shesha.Core", "Bank")]
        public virtual int? Bank { get; set; }

        [StringLength(100)]
        public virtual string BranchName { get; set; }

        [StringLength(10)]
        
        public virtual string BranchCode { get; set; }

        [ReferenceList("Shesha.Core", "BankAccountType")]
        public virtual int? AccountType { get; set; }

        [StringLength(20)]
        public virtual string AccountNumber { get; set; }

        [StringLength(50)]
        public virtual string OtherAccountType { get; set; }
        public virtual int? TenantId { get; set; }
    }
}
