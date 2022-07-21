using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Authorization.Users;
using Abp.Extensions;
using Shesha.Domain.Enums;
using Shesha.EntityHistory;

namespace Shesha.Authorization.Users
{
    [Table("AbpUsers")]
    public class User : AbpUser<User>
    {
        public const string DefaultPassword = "123qwe";

        public User()
        {
            Claims = new List<UserClaim>();
            Roles = new List<UserRole>();
            TypeOfAccount = RefListTypeOfAccount.Internal;
        }

        public virtual DateTime? LastLoginDate { get; set; }

        public static string CreateRandomPassword()
        {
            return Guid.NewGuid().ToString("N").Truncate(16);
        }

        /// <summary>
        /// Email address of the user.
        /// Email address must be unique for it's tenant.
        /// </summary>
        [StringLength(MaxEmailAddressLength)]
        public override string EmailAddress { get; set; }         

        public override void SetNormalizedNames()
        {
            NormalizedUserName = UserName.ToUpperInvariant();
            NormalizedEmailAddress = EmailAddress?.ToUpperInvariant();
        }

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

        /// <summary>
        /// Is this user active?
        /// If as user is not active, he/she can not use the application.
        /// </summary>
        [AuditedAsEvent(typeof(IsLockedEventCreator))]
        public override bool IsActive { get; set; }

        [Display(Name = "Type of account")]
        public virtual RefListTypeOfAccount? TypeOfAccount { get; set; }

        public static User CreateTenantAdminUser(int tenantId, string emailAddress)
        {
            var user = new User
            {
                TenantId = tenantId,
                UserName = AdminUserName,
                Name = AdminUserName,
                Surname = AdminUserName,
                EmailAddress = emailAddress,
                Roles = new List<UserRole>(),
                TypeOfAccount = RefListTypeOfAccount.Internal,
            };

            user.SetNormalizedNames();

            return user;
        }

        private class IsLockedEventCreator : EntityHistoryEventCreatorBase<User, bool>
        {
            public override EntityHistoryEventInfo CreateEvent(EntityChangesInfo<User, bool> change)
            {
                var text = change.NewValue ? "User locked" : "User unlocked";
                return CreateEvent(text, text);
            }
        }
    }
}
