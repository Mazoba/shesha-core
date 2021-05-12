using System;
using Abp.Domain.Entities.Auditing;
using Shesha.Domain.Attributes;

namespace Shesha.Domain
{
    
    [Entity(FriendlyName = "Logon Message Audit Item", TypeShortAlias = "Shesha.Core.LogonMessageAuditItem")]
    public class LogonMessageAuditItem : CreationAuditedEntity<Guid>
    {
        public virtual LogonMessage Message { get; set; }
        public virtual Person Person { get; set; }
        public virtual bool DontShowAgain { get; set; }
    }
}
