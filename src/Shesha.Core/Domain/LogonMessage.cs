using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using Shesha.Domain.Attributes;
using Shesha.Domain.Enums;

namespace Shesha.Domain
{
    [Entity(FriendlyName = "Logon Message", TypeShortAlias = "Shesha.Core.LogonMessage")]
    public class LogonMessage : FullAuditedEntity<Guid>, IMayHaveTenant
    {
        public LogonMessage()
        {
            IsActive = true;
            Visibility = RefListLogonMessageVisibility.AllUsers;
        }

        [Required]
        [StringLength(500)]
        public virtual string Description { get; set; }

        [Required]
        [DataType(DataType.Html)]
        [StringLength(int.MaxValue)]
        public virtual string Content { get; set; }

        [Required]
        public virtual DateTime? PublicationStartDate { get; set; }

        [Required]
        public virtual DateTime? PublicationEndDate { get; set; }

        public virtual DistributionList DistributionList { get; set; }

        public virtual RefListLogonMessageVisibility Visibility { get; set; }

        public virtual bool IsActive { get; set; }
        public virtual int? TenantId { get; set; }
    }
}
