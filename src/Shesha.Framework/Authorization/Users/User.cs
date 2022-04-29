using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Authorization.Users;
using Abp.Extensions;

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

        public static User CreateTenantAdminUser(int tenantId, string emailAddress)
        {
            var user = new User
            {
                TenantId = tenantId,
                UserName = AdminUserName,
                Name = AdminUserName,
                Surname = AdminUserName,
                EmailAddress = emailAddress,
                Roles = new List<UserRole>()
            };

            user.SetNormalizedNames();

            return user;
        }
    }
}
