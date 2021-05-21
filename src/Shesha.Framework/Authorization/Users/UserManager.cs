using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Authorization.Users;
using Abp.Configuration;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Organizations;
using Abp.Runtime.Caching;
using Abp.UI;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shesha.Authorization.Roles;

namespace Shesha.Authorization.Users
{
    public class UserManager : AbpUserManager<Role, User>
    {
        public UserManager(
            RoleManager roleManager,
            UserStore store, 
            IOptions<IdentityOptions> optionsAccessor, 
            IPasswordHasher<User> passwordHasher, 
            IEnumerable<IUserValidator<User>> userValidators, 
            IEnumerable<IPasswordValidator<User>> passwordValidators,
            ILookupNormalizer keyNormalizer, 
            IdentityErrorDescriber errors, 
            IServiceProvider services, 
            ILogger<UserManager<User>> logger, 
            IPermissionManager permissionManager, 
            IUnitOfWorkManager unitOfWorkManager, 
            ICacheManager cacheManager, 
            IRepository<OrganizationUnit, long> organizationUnitRepository, 
            IRepository<UserOrganizationUnit, long> userOrganizationUnitRepository, 
            IOrganizationUnitSettings organizationUnitSettings, 
            ISettingManager settingManager)
            : base(
                roleManager, 
                store, 
                optionsAccessor, 
                passwordHasher, 
                userValidators, 
                passwordValidators, 
                keyNormalizer, 
                errors, 
                services, 
                logger, 
                permissionManager, 
                unitOfWorkManager, 
                cacheManager,
                organizationUnitRepository, 
                userOrganizationUnitRepository, 
                organizationUnitSettings, 
                settingManager)
        {
        }

        /// <summary>
        /// Removes the specified <paramref name="user" /> from the named role.
        /// </summary>
        /// <param name="user">The user to remove from the named role.</param>
        /// <param name="role">The name of the role to remove the user from.</param>
        /// <returns>
        /// The <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation, containing the <see cref="T:Microsoft.AspNetCore.Identity.IdentityResult" />
        /// of the operation.
        /// </returns>
        //[DebuggerStepThrough]
        public override async Task<IdentityResult> RemoveFromRoleAsync(
            User user,
            string role)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            if (string.IsNullOrWhiteSpace(role))
                throw new ArgumentNullException(nameof(role));

            var roleEntity = await RoleManager.FindByNameAsync(role);
            if (roleEntity == null)
                return IdentityResult.Failed(new IdentityError() { Description = $"Role `{role}` not found" });

            var toRemove = user.Roles.Where(r => r.RoleId == roleEntity.Id).ToList();
            foreach (var roleToRemove in toRemove)
            {
                user.Roles.Remove(roleToRemove);
            }
            
            return IdentityResult.Success;
        }

        public override async Task<IdentityResult> SetRolesAsync(User user, string[] roleNames)
        {
            await AbpUserStore.UserRepository.EnsureCollectionLoadedAsync(user, u => u.Roles);

            //Remove from removed roles
            foreach (var userRole in user.Roles.ToList())
            {
                var role = await RoleManager.FindByIdAsync(userRole.RoleId.ToString());
                if (role != null && roleNames.All(roleName => !role.Name.Equals(roleName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    var result = await RemoveFromRoleAsync(user, role.Name);
                    if (!result.Succeeded)
                    {
                        return result;
                    }
                }
            }

            //Add to added roles
            foreach (var roleName in roleNames)
            {
                var role = await RoleManager.GetRoleByNameAsync(roleName);
                if (user.Roles.All(ur => ur.RoleId != role.Id))
                {
                    var result = await AddToRoleAsync(user, roleName);
                    if (!result.Succeeded)
                    {
                        return result;
                    }
                }
            }

            return IdentityResult.Success;
        }

        public override async Task<IdentityResult> CheckDuplicateUsernameOrEmailAddressAsync(long? expectedUserId, string userName, string emailAddress)
        {
            try
            {
                var normalizedUsername = NormalizeName(userName);
                var duplicate = await AbpUserStore.UserRepository.FirstOrDefaultAsync(u => u.NormalizedUserName == normalizedUsername && u.Id != expectedUserId);
                if (duplicate != null)
                {
                    throw new UserFriendlyException(string.Format(L("Identity.DuplicateUserName"), userName));
                }

                /* temporary disabled
                if (!string.IsNullOrWhiteSpace(emailAddress))
                {
                    var normalizedEmail = NormalizeEmail(emailAddress);

                    duplicate = await AbpUserStore.UserRepository.FirstOrDefaultAsync(u => u.NormalizedEmailAddress == normalizedEmail && u.Id != expectedUserId);
                    if (duplicate != null)
                    {
                        throw new UserFriendlyException(string.Format(L("Identity.DuplicateEmail"), emailAddress));
                    }
                }
                */

                return IdentityResult.Success;
            }
            catch
            {
                throw;
            }
        }

        // permissions
        public override Task<bool> IsGrantedAsync(User user, Permission permission)
        {
            return base.IsGrantedAsync(user, permission);
        }
    }
}
