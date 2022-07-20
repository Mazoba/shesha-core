using Shesha.EntityHistory;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shesha.Domain
{
    public class ShaUser
    {
        public virtual string SecurityPin { get; set; }

        [Display(Name = "Authentication Guid")]
        [StringLength(36)]
        public virtual string AuthenticationGuid { get; set; }

        [Display(Name = "Authentication Guid Expiry Date")]
        public virtual DateTime? AuthenticationGuidExpiresOn { get; set; }

        /// <summary>
        /// One Time Passwords by SMS
        /// </summary>
        [DisplayFormat(DataFormatString = "Yes|No")]
        [Display(Name = "Use SMS Based One-Time-Passwords")]
        [AuditedBoolean("SMS Based One-Time-Passwords enabled", "SMS Based One-Time-Passwords disabled")]
        public virtual bool OtpEnabled { get; set; }

        [Display(Name = "Require a change of password")]
        public virtual bool RequireChangePassword { get; set; }

        [Display(Name = "Account is locked")]
        [AuditedAsEvent(typeof(IsLockedEventCreator))]
        public virtual bool IsLocked { get; set; }

        private class IsLockedEventCreator : EntityHistoryEventCreatorBase<ShaUser, bool>
        {
            public override EntityHistoryEventInfo CreateEvent(EntityChangesInfo<ShaUser, bool> change)
            {
                var text = change.NewValue ? "User locked" : "User unlocked";
                return CreateEvent(text, text);
            }
        }
    }
}
